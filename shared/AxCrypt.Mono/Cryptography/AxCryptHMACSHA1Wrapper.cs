using AxCrypt.Abstractions.Algorithm;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AxCrypt.Mono.Cryptography
{
    internal class AxCryptHMACSHA1Wrapper : AxCrypt.Abstractions.Algorithm.AxCryptHMACSHA1
    {
        private System.Security.Cryptography.KeyedHashAlgorithm _hmac;

        public AxCryptHMACSHA1Wrapper(System.Security.Cryptography.KeyedHashAlgorithm hmac)
        {
            _hmac = hmac;
            _hashName = nameof(AxCryptHMACSHA1Wrapper);
        }

        private string _hashName;

        public override string HashName { get { return _hashName; } set { _hashName = value; } }

        public override byte[] Key()
        {
            return _hmac.Key;
        }

        public override void SetKey(byte[] value)
        {
            _hmac.Key = EnsureBlockSizeForKeyDueToBugInMonoKeyPropertySetter(value);
        }

        private static byte[] EnsureBlockSizeForKeyDueToBugInMonoKeyPropertySetter(byte[] key)
        {
            if (key.Length <= 20)
            {
                return key;
            }
            return System.Security.Cryptography.SHA1.Create().ComputeHash(key);
        }

        public override byte[] ComputeHash(byte[] buffer)
        {
            return _hmac.ComputeHash(buffer);
        }

        public override byte[] ComputeHash(byte[] buffer, int offset, int count)
        {
            return _hmac.ComputeHash(buffer, offset, count);
        }

        public override byte[] ComputeHash(System.IO.Stream inputStream)
        {
            return _hmac.ComputeHash(inputStream);
        }

        public override byte[] Hash()
        {
            return _hmac.Hash;
        }

        public override int HashSize
        {
            get { return _hmac.HashSize; }
        }

        public override void Initialize()
        {
            _hmac.Initialize();
        }

        public override HMAC Initialize(ISymmetricKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            Initialize();
            SetKey(key.GetBytes());
            return this;
        }

        public override bool CanReuseTransform
        {
            get { return _hmac.CanReuseTransform; }
        }

        public override bool CanTransformMultipleBlocks
        {
            get { return _hmac.CanTransformMultipleBlocks; }
        }

        public override int InputBlockSize
        {
            get { return _hmac.InputBlockSize; }
        }

        public override int OutputBlockSize
        {
            get { return _hmac.OutputBlockSize; }
        }

        public override int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            return _hmac.TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
        }

        public override byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            return _hmac.TransformFinalBlock(inputBuffer, inputOffset, inputCount);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hmac.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
