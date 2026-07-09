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

using AxCrypt.Abstractions;

namespace AxCrypt.Cli
{
    /// <summary>
    /// A trivial IUIThread for a console process: there is no UI thread, so all
    /// delegates are executed synchronously on the calling thread.
    /// </summary>
    internal sealed class CliUIThread : IUIThread
    {
        public bool Blocked { get; set; }

        public bool IsOn => true;

        public void Yield()
        {
        }

        public void ExitApplication()
        {
            Environment.Exit(0);
        }

        public void RestartApplication()
        {
            throw new NotSupportedException("The command-line utility cannot restart itself.");
        }

        public void SendTo(Action action)
        {
            action();
        }

        public Task SendToAsync(Func<Task> action)
        {
            return action();
        }

        public void PostTo(Action action)
        {
            action();
        }
    }
}
