﻿using System;
using System.Runtime.Serialization;

namespace AssemblyForTests
{
    public class SomeClass
    {
        public SomeClass(ISerializable serializable, int param, ICloneable cloneable)
        {

        }

        public int FirstMethod(int foo, string bar)
        {
            return foo;
        }

        public void SecondMethod(ISerializable serializable)
        {
            return;
        }
    }

    public class SecondClass
    {
        public String ToString()
        {
            return "";
        }

        public bool ValidateNumbers(int firstNumber, int secondNumber)
        {
            return firstNumber != secondNumber;
        }
    }
}
