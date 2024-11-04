// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Azure.SignalRBench.Storage
{
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
    public sealed class ComparableString
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
    {
        public ComparableString(object value) => Value = value;

        public object Value { get; }

        public static explicit operator ComparableString(string value) => new ComparableString(value);

        public static bool operator >(string value, ComparableString columnValue)
        {
            return StringComparer.Ordinal.Compare(value, columnValue?.Value as string) > 0;
        }

        public static bool operator <(string value, ComparableString columnValue)
        {
            return StringComparer.Ordinal.Compare(value, columnValue?.Value as string) < 0;
        }

        public static bool operator ==(string value, ComparableString columnValue)
        {
            return value == (columnValue?.Value as string);
        }

        public static bool operator !=(string value, ComparableString columnValue)
        {
            return value != (columnValue?.Value as string);
        }

        public static bool operator >=(string value, ComparableString columnValue)
        {
            return StringComparer.Ordinal.Compare(value, columnValue?.Value as string) >= 0;
        }

        public static bool operator <=(string value, ComparableString columnValue)
        {
            return StringComparer.Ordinal.Compare(value, columnValue?.Value as string) <= 0;
        }
    }
}