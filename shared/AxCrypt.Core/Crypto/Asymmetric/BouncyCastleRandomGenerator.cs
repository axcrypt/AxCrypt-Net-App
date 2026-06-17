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

using Org.BouncyCastle.Security;
using System;

namespace AxCrypt.Core.Crypto.Asymmetric
{
    internal class BouncyCastleRandomGenerator : Org.BouncyCastle.Crypto.Prng.IRandomGenerator
    {
        public void AddSeedMaterial(byte[] seed)
        {
        }

        public void AddSeedMaterial(ReadOnlySpan<byte> seed)
        {
        }

        public void AddSeedMaterial(long seed)
        {
        }

        public void NextBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            System.Security.Cryptography.RandomNumberGenerator.Fill(buffer);
        }

        public void NextBytes(byte[] buffer, int start, int len)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (start < 0 || start > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            if (len < 0 || len > buffer.Length - start)
            {
                throw new ArgumentOutOfRangeException(nameof(len));
            }

            System.Security.Cryptography.RandomNumberGenerator.Fill(buffer.AsSpan(start, len));
        }

        public void NextBytes(Span<byte> buffer)
        {
            System.Security.Cryptography.RandomNumberGenerator.Fill(buffer);
        }

        public static SecureRandom CreateSecureRandom()
        {
            return new SecureRandom(new BouncyCastleRandomGenerator());
        }
    }
}
