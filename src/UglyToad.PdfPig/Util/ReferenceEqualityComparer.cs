namespace UglyToad.PdfPig.Util
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Compares instances by reference, ignoring any overridden <see cref="object.Equals(object)"/> and
    /// <see cref="object.GetHashCode"/>. Useful for caching against tokens, whose value equality walks the
    /// whole token tree and is therefore too expensive to use as a dictionary key.
    /// </summary>
    internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();

        private ReferenceEqualityComparer()
        {
        }

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
