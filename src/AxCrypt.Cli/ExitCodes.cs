// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) AxCrypt AB
//
// This file is part of AxCrypt.
//
// AxCrypt is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// AxCrypt is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with AxCrypt. If not, see <https://www.gnu.org/licenses/>.

namespace AxCrypt.Cli
{
    /// <summary>
    /// Stable exit codes for scripting and automation. See docs/CLI.md.
    /// </summary>
    public static class ExitCodes
    {
        public const int Success = 0;

        public const int GeneralError = 1;

        public const int UsageError = 2;

        public const int WrongPasswordOrKey = 3;

        public const int FileNotFound = 4;

        public const int InvalidOrCorruptFile = 5;
    }
}
