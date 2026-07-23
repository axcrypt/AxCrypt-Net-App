#region Coypright and License

/*
 * AxCrypt - Copyright 2016, Svante Seleborg, All Rights Reserved
 *
 * This file is part of AxCrypt.
 *
 * AxCrypt is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * AxCrypt is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with AxCrypt.  If not, see <http://www.gnu.org/licenses/>.
 *
 * The source is maintained at http://bitbucket.org/AxCrypt-net please visit for
 * updates, contributions and contact with the author. You may also visit
 * http://www.axcrypt.net for more information about the author.
*/

#endregion Coypright and License

using System;
using System.Diagnostics;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Crypto
{
    public class IterationCalculator
    {
        private class WrapIterator
        {
            private ICrypto _dummyCrypto;

            private Salt _dummySalt;

            private SymmetricKey _dummyKey;

            public WrapIterator(Guid cryptoId)
            {
                ICryptoFactory factory = Resolve.CryptoFactory.Create(cryptoId);
                _dummyCrypto = factory.CreateCrypto(factory.CreateDerivedKey(new Passphrase("A dummy passphrase")).DerivedKey, null, 0);
                _dummySalt = new Salt(_dummyCrypto.Key.Size);
                _dummyKey = new SymmetricKey(_dummyCrypto.Key.Size);
            }

            public void Iterate(long keyWrapIterations)
            {
                KeyWrap keyWrap = new KeyWrap(_dummySalt, keyWrapIterations, KeyWrapMode.Specification);
                keyWrap.Wrap(_dummyCrypto, _dummyKey);
            }
        }

        // Target roughly half a second of key-wrap work on the encrypting device (previously ~1/20 s),
        // with a substantially higher floor. The resulting count is stored per file and cached per
        // install, so raising these values strengthens newly written files and freshly calibrated
        // installs without affecting existing files or already-calibrated installs.
        private const long TargetWorkFactorDivisor = 2;      // iterationsPerSecond / 2 ~= 0.5 seconds

        private const long MinimumKeyWrapIterations = 20000;

        /// <summary>
        /// Get the number of key wrap iterations we use by default. This is a calculated value intended to cause the wrapping
        /// operation to take approximately half a second on the system where the code is run.
        /// A minimum of <see cref="MinimumKeyWrapIterations"/> iterations is always guaranteed.
        /// </summary>
        /// <param name="cryptoId">The id of the crypto to use for the wrap.</param>
        public virtual long KeyWrapIterations(Guid cryptoId)
        {
            // Do the one-off setup (which performs a key derivation of its own) BEFORE starting the
            // clock, then time only the measurement loop. Previously the setup ran inside the timed
            // window, which on slow devices could exhaust the 500 ms budget after a single batch,
            // under-measuring iterations/second and weakening the KDF.
            WrapIterator wrapIterator = new WrapIterator(cryptoId);

            Stopwatch stopwatch = Stopwatch.StartNew();
            long iterationsPerSecond = IterationsPerSecond(stopwatch, wrapIterator.Iterate);
            long defaultIterations = iterationsPerSecond / TargetWorkFactorDivisor;

            if (defaultIterations < MinimumKeyWrapIterations)
            {
                defaultIterations = MinimumKeyWrapIterations;
            }

            return defaultIterations;
        }

        private static long IterationsPerSecond(Stopwatch stopwatch, Action<long> iterate)
        {
            long iterationsIncrement = 1000;
            long totalIterations = 0;
            do
            {
                iterate(iterationsIncrement);
                totalIterations += iterationsIncrement;
            } while (stopwatch.ElapsedMilliseconds < 500);
            long elapsedMilliseconds = Math.Max(1, stopwatch.ElapsedMilliseconds);
            long iterationsPerSecond = totalIterations * 1000 / elapsedMilliseconds;
            return iterationsPerSecond;
        }
    }
}
