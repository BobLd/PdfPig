namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using UglyToad.PdfPig.Core;

    // for kd-tree with line segments, see https://stackoverflow.com/questions/14376679/how-to-represent-line-segments-in-kd-tree 

    /// <summary>
    /// K-D tree data structure of <see cref="PdfPoint"/>.
    /// </summary>
    public class KdTree : KdTree<PdfPoint>
    {
        /// <summary>
        /// K-D tree data structure of <see cref="PdfPoint"/>.
        /// </summary>
        /// <param name="points">The points used to build the tree.</param>
        public KdTree(IReadOnlyList<PdfPoint> points) : base(points, p => p)
        { }

        /// <summary>
        /// Get the nearest neighbour to the pivot point.
        /// Only returns 1 neighbour, even if equidistant points are found.
        /// </summary>
        /// <param name="pivot">The point for which to find the nearest neighbour.</param>
        /// <param name="distanceMeasure">The distance measure used, e.g. the Euclidian distance.</param>
        /// <param name="index">The nearest neighbour's index (returns -1 if not found).</param>
        /// <param name="distance">The distance between the pivot and the nearest neighbour (returns <see cref="double.NaN"/> if not found).</param>
        /// <returns>The nearest neighbour's point.</returns>
        public PdfPoint FindNearestNeighbour(PdfPoint pivot, Func<PdfPoint, PdfPoint, double> distanceMeasure, out int index, out double distance)
        {
            return FindNearestNeighbour(pivot, p => p, distanceMeasure, out index, out distance);
        }

        /// <summary>
        /// Get the k nearest neighbours to the pivot point.
        /// Might return more than k neighbours if points are equidistant.
        /// <para>Use <see cref="FindNearestNeighbour(PdfPoint, Func{PdfPoint, PdfPoint, double}, out int, out double)"/> if only looking for the (single) closest point.</para>
        /// </summary>
        /// <param name="pivot">The point for which to find the nearest neighbour.</param>
        /// <param name="k">The number of neighbours to return. Might return more than k neighbours if points are equidistant.</param>
        /// <param name="distanceMeasure">The distance measure used, e.g. the Euclidian distance.</param>
        /// <returns>Returns a list of tuples of the k nearest neighbours. Tuples are (element, index, distance).</returns>
        public IReadOnlyList<(PdfPoint, int, double)> FindNearestNeighbours(PdfPoint pivot, int k, Func<PdfPoint, PdfPoint, double> distanceMeasure)
        {
            return FindNearestNeighbours(pivot, k, p => p, distanceMeasure);
        }
    }

    /// <summary>
    /// K-D tree data structure.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class KdTree<T>
    {
        private readonly KdTreeComparerY kdTreeComparerY = new KdTreeComparerY();
        private readonly KdTreeComparerX kdTreeComparerX = new KdTreeComparerX();

        /// <summary>
        /// The root of the tree.
        /// </summary>
        public readonly KdTreeNode<T> Root;

        /// <summary>
        /// Number of elements in the tree.
        /// </summary>
        public readonly int Count;

        /// <summary>
        /// K-D tree data structure.
        /// </summary>
        /// <param name="elements">The elements used to build the tree.</param>
        /// <param name="elementsPointFunc">The function that converts the candidate elements into a <see cref="PdfPoint"/>.</param>
        public KdTree(IReadOnlyList<T> elements, Func<T, PdfPoint> elementsPointFunc)
        {
            if (elements == null || elements.Count == 0)
            {
                throw new ArgumentException("KdTree(): candidates cannot be null or empty.", nameof(elements));
            }

            Count = elements.Count;

            KdTreeElement<T>[] array = new KdTreeElement<T>[Count];

            for (int i = 0; i < Count; i++)
            {
                var el = elements[i];
                array[i] = new KdTreeElement<T>(i, elementsPointFunc(el), el);
            }

#if NET6_0_OR_GREATER
            Root = BuildTree(new Span<KdTreeElement<T>>(array));
#else
            Root = BuildTree(new ArraySegment<KdTreeElement<T>>(array));
#endif
        }

#if NET6_0_OR_GREATER
        private KdTreeNode<T> BuildTree(Span<KdTreeElement<T>> P, int depth = 0)
        {
            if (P.Length == 0)
            {
                return null;
            }

            if (P.Length == 1)
            {
                return KdTreeNode<T>.CreateLeaf(P[0], depth);
            }

            if (depth % 2 == 0)
            {
                P.Sort(kdTreeComparerX);
            }
            else
            {
                P.Sort(kdTreeComparerY);
            }

            if (P.Length == 2)
            {
                return KdTreeNode<T>.CreateNode(KdTreeNode<T>.CreateLeaf(P[0], depth + 1), null, P[1], depth);
            }

            int median = P.Length / 2;

            KdTreeNode<T> vLeft = BuildTree(P.Slice(0, median), depth + 1);
            KdTreeNode<T> vRight = BuildTree(P.Slice(median + 1), depth + 1);

            return KdTreeNode<T>.CreateNode(vLeft, vRight, P[median], depth);
        }
#else
        private KdTreeNode<T> BuildTree(ArraySegment<KdTreeElement<T>> P, int depth = 0)
        {
            if (P.Count == 0)
            {
                return null;
            }

            if (P.Count == 1)
            {
                return KdTreeNode<T>.CreateLeaf(P.GetAt(0), depth);
            }

            if (depth % 2 == 0)
            {
                P.Sort(kdTreeComparerX);
            }
            else
            {
                P.Sort(kdTreeComparerY);
            }

            if (P.Count == 2)
            {
                return  KdTreeNode<T>.CreateNode(KdTreeNode<T>.CreateLeaf(P.GetAt(0), depth + 1), null, P.GetAt(1), depth);
            }

            int median = P.Count / 2;

            KdTreeNode<T> vLeft = BuildTree(P.Take(median), depth + 1);
            KdTreeNode<T> vRight = BuildTree(P.Skip(median + 1), depth + 1);

            return KdTreeNode<T>.CreateNode(vLeft, vRight, P.GetAt(median), depth);
        }
#endif

        #region NN
        /// <summary>
        /// Get the nearest neighbour to the pivot element.
        /// Only returns 1 neighbour, even if equidistant points are found.
        /// </summary>
        /// <param name="pivot">The element for which to find the nearest neighbour.</param>
        /// <param name="pivotPointFunc">The function that converts the pivot element into a <see cref="PdfPoint"/>.</param>
        /// <param name="distanceMeasure">The distance measure used, e.g. the Euclidian distance.</param>
        /// <param name="index">The nearest neighbour's index (returns -1 if not found).</param>
        /// <param name="distance">The distance between the pivot and the nearest neighbour (returns <see cref="double.NaN"/> if not found).</param>
        /// <returns>The nearest neighbour's element.</returns>
        public T FindNearestNeighbour(T pivot, Func<T, PdfPoint> pivotPointFunc, Func<PdfPoint, PdfPoint, double> distanceMeasure, out int index, out double distance)
        {
            var result = FindNearestNeighbour(Root, pivot, pivotPointFunc(pivot), distanceMeasure);

            if (result.Node is null)
            {
                index = -1;
                distance = double.NaN;
                return default;
            }
            
            index = result.Node.Index;
            distance = result.Distance ?? double.NaN;
            return result.Node.Element;
        }

        private readonly struct KdTreeResult
        {
            public KdTreeResult(KdTreeNode<T> node, double? distance)
            {
                Node = node;
                Distance = distance;
            }
            
            public readonly KdTreeNode<T> Node;

            public readonly double? Distance;
        }

        private static KdTreeResult FindNearestNeighbour(KdTreeNode<T> node, T pivot, PdfPoint pivotPoint, Func<PdfPoint, PdfPoint, double> distance)
        {
            if (node == null)
            {
                return new KdTreeResult(null, null);
            }

            if (node.IsLeaf())
            {
                if (node.Element.Equals(pivot))
                {
                    return new KdTreeResult(null, null);
                }
                return new KdTreeResult(node, distance(node.Value, pivotPoint));
            }
            
            var currentNearestNode = node;
            var currentDistance = distance(node.Value, pivotPoint);

            // Early exit condition: if the current distance is already 0, return immediately
            if (Math.Abs(currentDistance) < double.Epsilon && !currentNearestNode.Element.Equals(pivot))
            {
                return new KdTreeResult(currentNearestNode, currentDistance);
            }

            var pointValue = node.IsAxisCutX() ? pivotPoint.X : pivotPoint.Y;
            
            if (pointValue < node.L)
            {
                // start left
                KdTreeResult newResult = FindNearestNeighbour(node.LeftChild, pivot, pivotPoint, distance);

                if (newResult.Distance.HasValue && newResult.Distance.Value <= currentDistance && !newResult.Node.Element.Equals(pivot))
                {
                    currentDistance = newResult.Distance.Value;
                    currentNearestNode = newResult.Node;

                    // Early exit condition: if the updated distance is 0, return immediately
                    if (Math.Abs(currentDistance) < double.Epsilon)
                    {
                        return new KdTreeResult(currentNearestNode, currentDistance);
                    }
                }

                if (node.RightChild != null && pointValue + currentDistance >= node.L)
                {
                    newResult = FindNearestNeighbour(node.RightChild, pivot, pivotPoint, distance);

                    if (newResult.Distance.HasValue && newResult.Distance.Value < currentDistance && !newResult.Node.Element.Equals(pivot))
                    {
                        currentDistance = newResult.Distance.Value;
                        currentNearestNode = newResult.Node;
                        
                        // Early exit condition: if the updated distance is 0, return immediately
                        if (Math.Abs(currentDistance) < double.Epsilon)
                        {
                            return new KdTreeResult(currentNearestNode, currentDistance);
                        }
                    }
                }
            }
            else
            {
                // start right
                KdTreeResult newResult = FindNearestNeighbour(node.RightChild, pivot, pivotPoint, distance);

                if (newResult.Distance.HasValue && newResult.Distance.Value <= currentDistance && !newResult.Node.Element.Equals(pivot))
                {
                    currentDistance = newResult.Distance.Value;
                    currentNearestNode = newResult.Node;

                    // Early exit condition: if the updated distance is 0, return immediately
                    if (Math.Abs(currentDistance) < double.Epsilon)
                    {
                        return new KdTreeResult(currentNearestNode, currentDistance);
                    }
                }

                if (node.LeftChild != null && pointValue - currentDistance <= node.L)
                {
                    newResult = FindNearestNeighbour(node.LeftChild, pivot, pivotPoint, distance);
                    
                    if (newResult.Distance.HasValue && newResult.Distance < currentDistance && !newResult.Node.Element.Equals(pivot))
                    {
                        currentDistance = newResult.Distance.Value;
                        currentNearestNode = newResult.Node;
                        
                        // Early exit condition: if the updated distance is 0, return immediately
                        if (Math.Abs(currentDistance) < double.Epsilon)
                        {
                            return new KdTreeResult(currentNearestNode, currentDistance);
                        }
                    }
                }
            }

            return new KdTreeResult(currentNearestNode, currentDistance);
        }
        #endregion

        #region k-NN
        /// <summary>
        /// Get the k nearest neighbours to the pivot element.
        /// Might return more than k neighbours if points are equidistant.
        /// <para>Use <see cref="FindNearestNeighbour(KdTreeNode{T}, T, Func{T, PdfPoint}, Func{PdfPoint, PdfPoint, double})"/> if only looking for the (single) closest point.</para>
        /// </summary>
        /// <param name="pivot">The element for which to find the k nearest neighbours.</param>
        /// <param name="k">The number of neighbours to return. Might return more than k neighbours if points are equidistant.</param>
        /// <param name="pivotPointFunc">The function that converts the pivot element into a <see cref="PdfPoint"/>.</param>
        /// <param name="distanceMeasure">The distance measure used, e.g. the Euclidian distance.</param>
        /// <returns>Returns a list of tuples of the k nearest neighbours. Tuples are (element, index, distance).</returns>
        public IReadOnlyList<(T, int, double)> FindNearestNeighbours(T pivot, int k, Func<T, PdfPoint> pivotPointFunc, Func<PdfPoint, PdfPoint, double> distanceMeasure)
        {
            PdfPoint pivotPoint = pivotPointFunc(pivot);
            var kdTreeNodes = new KNearestNeighboursQueue(k);
            FindNearestNeighbours(Root, pivot, k, pivotPoint, distanceMeasure, kdTreeNodes);
            return kdTreeNodes.SelectMany(n => n.Value.Select(e => (e.Element, e.Index, n.Key))).ToArray();
        }

        private static KdTreeResult FindNearestNeighbours(KdTreeNode<T> node, T pivot, int k,
            PdfPoint pivotPoint, Func<PdfPoint, PdfPoint, double> distance, KNearestNeighboursQueue queue)
        {
            if (node == null)
            {
                return new KdTreeResult(null, double.NaN);
            }

            if (node.IsLeaf())
            {
                if (node.Element.Equals(pivot))
                {
                    return new KdTreeResult(null, double.NaN);
                }

                var currentDistance = distance(node.Value, pivotPoint);
                var currentNearestNode = node;

                if (!queue.IsFull || currentDistance <= queue.LastDistance)
                {
                    queue.Add(currentDistance, currentNearestNode);
                    currentDistance = queue.LastDistance;
                    currentNearestNode = queue.LastElement;
                }

                return new KdTreeResult(currentNearestNode, currentDistance);
            }
            else
            {
                var currentNearestNode = node;
                var currentDistance = distance(node.Value, pivotPoint);
                if ((!queue.IsFull || currentDistance <= queue.LastDistance) && !node.Element.Equals(pivot))
                {
                    queue.Add(currentDistance, currentNearestNode);
                    currentDistance = queue.LastDistance;
                    currentNearestNode = queue.LastElement;
                }

                var pointValue = node.IsAxisCutX() ? pivotPoint.X : pivotPoint.Y;
                KdTreeResult newResult;

                if (pointValue < node.L)
                {
                    // start left
                    newResult = FindNearestNeighbours(node.LeftChild, pivot, k, pivotPoint, distance, queue);

                    if (newResult.Distance.HasValue && newResult.Distance.Value <= currentDistance && !newResult.Node.Element.Equals(pivot))
                    {
                        queue.Add(newResult.Distance.Value, newResult.Node);
                        currentDistance = queue.LastDistance;
                        currentNearestNode = queue.LastElement;
                    }

                    if (node.RightChild != null && pointValue + currentDistance >= node.L)
                    {
                        newResult = FindNearestNeighbours(node.RightChild, pivot, k, pivotPoint, distance, queue);
                    }
                }
                else
                {
                    // start right
                    newResult = FindNearestNeighbours(node.RightChild, pivot, k, pivotPoint, distance, queue);

                    if (newResult.Distance.HasValue && newResult.Distance.Value <= currentDistance && !newResult.Node.Element.Equals(pivot))
                    {
                        queue.Add(newResult.Distance.Value, newResult.Node);
                        currentDistance = queue.LastDistance;
                        currentNearestNode = queue.LastElement;
                    }

                    if (node.LeftChild != null && pointValue - currentDistance <= node.L)
                    {
                        newResult = FindNearestNeighbours(node.LeftChild, pivot, k, pivotPoint, distance, queue);
                    }
                }

                if (newResult.Distance.HasValue && newResult.Distance.Value <= currentDistance && !newResult.Node.Element.Equals(pivot))
                {
                    queue.Add(newResult.Distance.Value, newResult.Node);
                    currentDistance = queue.LastDistance;
                    currentNearestNode = queue.LastElement;
                }

                return new KdTreeResult(currentNearestNode, currentDistance);
            }
        }

        private sealed class KNearestNeighboursQueue : SortedList<double, HashSet<KdTreeNode<T>>>
        {
            public readonly int K;

            public KdTreeNode<T> LastElement { get; private set; }

            public double LastDistance { get; private set; }

            public bool IsFull => Count >= K;

            public KNearestNeighboursQueue(int k) : base(k)
            {
                K = k;
                LastDistance = double.PositiveInfinity;
            }

            public void Add(double key, KdTreeNode<T> value)
            {
                if (key > LastDistance && IsFull)
                {
                    return;
                }

                if (!ContainsKey(key))
                {
                    base.Add(key, new HashSet<KdTreeNode<T>>());
                    if (Count > K)
                    {
                        RemoveAt(Count - 1);
                    }
                }

                if (this[key].Add(value))
                {
                    var last = this.Last();
                    LastElement = last.Value.Last();
                    LastDistance = last.Key;
                }
            }
        }
        #endregion

        internal readonly struct KdTreeElement<R>
        {
            internal KdTreeElement(int index, PdfPoint point, R value)
            {
                Index = index;
                Value = point;
                Element = value;
            }

            public int Index { get; }

            public PdfPoint Value { get; }

            public R Element { get; }
        }

        private sealed class KdTreeComparerY : IComparer<KdTreeElement<T>>
        {
            public int Compare(KdTreeElement<T> p0, KdTreeElement<T> p1)
            {
                return p0.Value.Y.CompareTo(p1.Value.Y);
            }
        }

        private sealed class KdTreeComparerX : IComparer<KdTreeElement<T>>
        {
            public int Compare(KdTreeElement<T> p0, KdTreeElement<T> p1)
            {
                return p0.Value.X.CompareTo(p1.Value.X);
            }
        }
        
        /// <summary>
        /// K-D tree node.
        /// </summary>
        /// <typeparam name="Q"></typeparam>
        public sealed class KdTreeNode<Q>
        {
            internal static KdTreeNode<Q> CreateLeaf(KdTreeElement<Q> point, int depth)
            {
                return new KdTreeNode<Q>(null, null, point, depth);
            }

            internal static KdTreeNode<Q> CreateNode(KdTreeNode<Q> leftChild, KdTreeNode<Q> rightChild, KdTreeElement<Q> point, int depth)
            {
                return new KdTreeNode<Q>(leftChild, rightChild, point, depth);
            }

            /// <summary>
            /// Split value (X or Y axis).
            /// </summary>
            public double L => IsAxisCutX() ? Value.X : Value.Y;

            /// <summary>
            /// Split point.
            /// </summary>
            public PdfPoint Value { get; }

            /// <summary>
            /// Left child.
            /// </summary>
            public KdTreeNode<Q> LeftChild { get; }

            /// <summary>
            /// Right child.
            /// </summary>
            public KdTreeNode<Q> RightChild { get; }

            /// <summary>
            /// The node's element.
            /// </summary>
            public Q Element { get; }

            /// <summary>
            /// True if this cuts with X axis, false if cuts with Y axis.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsAxisCutX()
            {
                return Depth % 2 == 0;
            }

            /// <summary>
            /// The element's depth in the tree.
            /// </summary>
            public int Depth { get; }

            /// <summary>
            /// Return true if leaf.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsLeaf()
            {
                return LeftChild == null && RightChild == null;
            }

            /// <summary>
            /// The index of the element in the original array.
            /// </summary>
            public int Index { get; }

            private KdTreeNode(KdTreeNode<Q> leftChild, KdTreeNode<Q> rightChild, KdTreeElement<Q> point, int depth)
            {
                LeftChild = leftChild;
                RightChild = rightChild;
                Value = point.Value;
                Element = point.Element;
                Depth = depth;
                Index = point.Index;
            }

            /// <summary>
            /// Get the leaves.
            /// </summary>
            public IEnumerable<KdTreeNode<Q>> GetLeaves()
            {
                var leaves = new List<KdTreeNode<Q>>();
                RecursiveGetLeaves(LeftChild, ref leaves);
                RecursiveGetLeaves(RightChild, ref leaves);
                return leaves;
            }

            private static void RecursiveGetLeaves(KdTreeNode<Q> leaf, ref List<KdTreeNode<Q>> leaves)
            {
                if (leaf == null)
                {
                    return;
                }

                if (leaf.IsLeaf())
                {
                    leaves.Add(leaf);
                }
                else
                {
                    RecursiveGetLeaves(leaf.LeftChild, ref leaves);
                    RecursiveGetLeaves(leaf.RightChild, ref leaves);
                }
            }

            /// <inheritdoc />
            public override string ToString()
            {
                return "Node->" + Value.ToString();
            }
        }
    }
}
