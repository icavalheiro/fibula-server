﻿// -----------------------------------------------------------------
// <copyright file="IBytesInfo.cs" company="2Dudes">
// Copyright (c) 2018 2Dudes. All rights reserved.
// Author: Jose L. Nunez de Caceres
// jlnunez89@gmail.com
// http://linkedin.com/in/jlnunez89
//
// Licensed under the MIT license.
// See LICENSE.txt file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------

namespace Fibula.Communications.Contracts.Abstractions
{
    /// <summary>
    /// Interface for information in the form of a bytes array.
    /// </summary>
    public interface IBytesInfo : IIncomingPacket
    {
        /// <summary>
        /// Gets the information bytes.
        /// </summary>
        byte[] Bytes { get; }
    }
}
