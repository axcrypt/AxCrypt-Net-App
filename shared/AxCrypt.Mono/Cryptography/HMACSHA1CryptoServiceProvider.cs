#region Coypright and License

/*
 * AxCrypt AB - Copyright 2025, All Rights Reserved
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
 * The source is maintained at https://github.com/axcrypt/axcrypt-net-app please visit for
 * updates, contributions and contact with the author. You may also visit
 * http://www.axcrypt.net for more information about the author.
*/

#endregion Coypright and License

namespace AxCrypt.Mono.Cryptography
{
    public class HMACSHA1CryptoServiceProvider : AxCrypt.Mono.Cryptography.HMACBase
    {
        public HMACSHA1CryptoServiceProvider()
        {
            SetHash1(System.Security.Cryptography.SHA1.Create());
            SetHash2(System.Security.Cryptography.SHA1.Create());
            HashSizeValue = 160;
            BlockSizeValue = 20;
        }
    }
}