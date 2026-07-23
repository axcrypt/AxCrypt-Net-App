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

using System.Reflection;
using AxCrypt.Core.Runtime;

#endregion Coypright and License

using System;
using System.Collections.Generic;
using System.Linq;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Crypto
{
    public class CryptoFactory
    {
        // PBKDF2-HMAC-SHA512 iteration count used to derive the key-encrypting key from a passphrase.
        // Raised from the legacy 1000 to align with modern guidance (OWASP recommends on the order of
        // 210,000 for PBKDF2-HMAC-SHA512).
        //
        // Backward compatibility: this value is written into each file's key-wrap header
        // (V2KeyWrapHeaderBlock.DerivationIterations / DerivationSalt) and read back on decryption
        // (V2AxCryptDocument.Load -> CryptoFactory.RestoreDerivedKey(..., keyWrap.DerivationIterations)).
        // Existing files therefore keep their own stored count and remain decryptable, and files
        // written with this value stay readable by older builds that also read the header.
        //
        // Caveat: the local passphrase thumbprint (SymmetricKeyThumbprint) is computed with this
        // constant rather than a stored value, so after upgrading, an install recomputes identity
        // thumbprints once; locally cached "recent files" key associations may need a single
        // re-login. No file becomes unreadable. A future format revision should adopt a memory-hard
        // KDF (Argon2id) via the BouncyCastle dependency.
        public static readonly int DerivationIterations = 210000;

        private Dictionary<Guid, CryptoFactoryCreator> _factories = new Dictionary<Guid, CryptoFactoryCreator>();

        public CryptoFactory()
        {
        }

        public CryptoFactory(IEnumerable<Assembly> extraAssemblies)
        {
            IEnumerable<Type> types = TypeDiscovery.Interface(typeof(ICryptoFactory), extraAssemblies);

            foreach (Type type in types)
            {
                Add(() => Activator.CreateInstance(type) as ICryptoFactory);
            }
        }

        public void Add(CryptoFactoryCreator factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException("factory");
            }

            lock (_factories)
            {
                _factories.Add(factory().CryptoId, factory);
            }
        }

        public bool TypeNameExists(string fullName)
        {
            lock (_factories)
            {
                return _factories.Any(c => c.Value().GetType().FullName == fullName);
            }
        }

        public ICryptoFactory Create(Guid id)
        {
            if (id == Guid.Empty)
            {
                return New<ICryptoPolicy>().DefaultCryptoFactory(_factories.Values);
            }
            CryptoFactoryCreator factory;
            lock (_factories)
            {
                if (_factories.TryGetValue(id, out factory))
                {
                    return factory();
                }
            }
            throw new ArgumentException("CryptoFactory not found.", "id");
        }

        public ICryptoFactory Create(ICryptoPolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException("policy");
            }

            lock (_factories)
            {
                return policy.DefaultCryptoFactory(_factories.Values.OrderByDescending(f => f().Priority));
            }
        }

        /// <summary>
        /// Return a list of CryptoId's in a suitable order of preference and relevance, to be used to
        /// try and match a passphrase against a file.
        /// </summary>
        /// <returns>A list of CryptoId's to try in the order provided.</returns>
        public IEnumerable<Guid> OrderedIds
        {
            get
            {
                Guid defaultId = Preferred.CryptoId;
                Guid legacyId = Legacy.CryptoId;

                List<Guid> orderedIds = new List<Guid>();
                orderedIds.Add(defaultId);
                lock (_factories)
                {
                    orderedIds.AddRange(_factories.Values.Where(f => f().CryptoId != defaultId && f().CryptoId != legacyId).Select(f => f().CryptoId));
                }
                orderedIds.Add(legacyId);

                return orderedIds;
            }
        }

        public ICryptoFactory Default(ICryptoPolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            lock (_factories)
            {
                return policy.DefaultCryptoFactory(_factories.Values.OrderByDescending(f => f().Priority));
            }
        }

        public ICryptoFactory Preferred
        {
            get
            {
                lock (_factories)
                {
                    return New<ISystemCryptoPolicy>().PreferredCryptoFactory(_factories.Values.OrderByDescending(f => f().Priority));
                }
            }
        }

        public ICryptoFactory Legacy
        {
            get
            {
                return Create(new V1Aes128CryptoFactory().CryptoId);
            }
        }

        public ICryptoFactory Minimum
        {
            get
            {
                return Create(new V2Aes128CryptoFactory().CryptoId);
            }
        }
    }
}