// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Azure.SignalRBench.Storage
{
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
    public sealed class ColumnValue
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
    {
        public ColumnValue(object value) => Value = value;

        public object Value { get; }

        public static explicit operator ColumnValue(string value) => new ColumnValue(value);

        public static bool operator >(string value, ColumnValue columnValue)
        {
            throw new NotSupportedException();
        }

        public static bool operator <(string value, ColumnValue columnValue)
        {
            throw new NotSupportedException();
        }

        public static bool operator ==(string value, ColumnValue columnValue)
        {
            throw new NotSupportedException();
        }

        public static bool operator !=(string value, ColumnValue columnValue)
        {
            throw new NotSupportedException();
        }

        public static bool operator >=(string value, ColumnValue columnValue)
        {
            throw new NotSupportedException();
        }

        public static bool operator <=(string value, ColumnValue columnValue)
        {
            throw new NotSupportedException();
        }
    }
}
