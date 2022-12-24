//
// Copyright (c) 2012 Canyala Innovation AB
//
// All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Canyala.Lagoon.Extensions;

namespace Canyala.Mercury.Storage.Internal
{
    /// <summary>
    /// Interface to implement serialization of primitive datatypes
    /// for heap allocators.
    /// </summary>
    internal interface ISerializer
    {
        /// <summary>
        /// Serializes a primitive value to a byte array.
        /// </summary>
        /// <param name="value">The value to serialize</param>
        /// <returns>A byte array representing the value.</returns>
        /// <remarks>
        /// The type of the parameter value is set to object here in the interface
        /// but will be cast to a particular type for each individual implementation 
        /// of this interface. So for example the ForString class that implements 
        /// string serialization will cast the object value to (string) allways.
        /// </remarks>
        byte[] Serialize(object value);

        /// <summary>
        /// Deserializes a byte array into a particular type of
        /// object.
        /// </summary>
        /// <param name="data">byte array to deserialize</param>
        /// <returns>A new object constructed from the bytes in the array.</returns>
        /// <remarks>
        /// The type of object that will be constructed is defined by the particular 
        /// ISerializer implementation class. So, we have serializers for ints, strings, doubles etc.
        /// </remarks>
        object Deserialize(byte[] data);
    }

    /// <summary>
    /// For serializing/deserializing primitive datatypes
    /// (currently string, int, long, double, bool, DateTime)
    /// </summary>
    internal class Serializer
    {
        /// <summary>
        /// Factory method to return a suitable serializer for a particular type (primitive)
        /// of values.
        /// </summary>
        /// <param name="t">Type of values to serialize/deserialize</param>
        /// <returns>An ISerializer implemnentation for the type specified.</returns>
        public static ISerializer SerializerFor(Type t)
        {
            if (t == typeof(string))
                return new ForString();

            else if (t == typeof(double))
                return new ForDouble();

            else if (t == typeof(DateTime))
                return new ForDateTime();

            else if (t == typeof(int))
                return new ForInt();

            else if (t == typeof(long))
                return new ForLong();

            else if (t == typeof(bool))
                return new ForBoolean();

            else
                throw new ArgumentException("Cannot create serializer for type {0}".Args(t));
        }

        /// <summary>
        /// Serializer implementation for strings.
        /// </summary>
        class ForString : ISerializer
        {
            public byte[] Serialize(object value)
            {
                var stream = new MemoryStream();
                (new BinaryWriter(stream, Encoding.UTF8)).Write((string)value);
                return stream.ToArray();
            }

            public object Deserialize(byte[] data)
            {
                var stream = new MemoryStream(data);
                return (new BinaryReader(stream, Encoding.UTF8)).ReadString(); 
            }
        }

        /// <summary>
        /// Serializer implementation for ints.
        /// </summary>
        class ForInt : ISerializer
        {
            public byte[] Serialize(object value)
                { return BitConverter.GetBytes((int)value); }

            public object Deserialize(byte[] data)
                { return BitConverter.ToInt32(data, 0); }
        }

        /// <summary>
        /// Serializer implementation for longs.
        /// </summary>
        class ForLong : ISerializer
        {
            public byte[] Serialize(object value)
                { return BitConverter.GetBytes((long)value); }

            public object Deserialize(byte[] data)
                { return BitConverter.ToInt64(data, 0); }
        }

        /// <summary>
        /// Serializer implementation for doubles.
        /// </summary>
        class ForDouble : ISerializer
        {
            public byte[] Serialize(object value)
                { return BitConverter.GetBytes((double)value); }

            public object Deserialize(byte[] data)
                { return BitConverter.ToDouble(data, 0); }
        }

        /// <summary>
        /// Serializer implementation for bools.
        /// </summary>
        class ForBoolean : ISerializer
        {
            public byte[] Serialize(object value)
                { return BitConverter.GetBytes((bool)value); }

            public object Deserialize(byte[] data)
                { return BitConverter.ToBoolean(data, 0); }
        }

        /// <summary>
        /// Serializer implementation for DateTimes.
        /// </summary>
        class ForDateTime : ISerializer
        {
            public byte[] Serialize(object value)
                { return BitConverter.GetBytes(((DateTime)value).ToBinary()); }

            public object Deserialize(byte[] data)
                { return DateTime.FromBinary(BitConverter.ToInt64(data, 0)); }    
        }
    }
}
