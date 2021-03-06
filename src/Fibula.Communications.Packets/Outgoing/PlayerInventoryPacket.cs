﻿// -----------------------------------------------------------------
// <copyright file="PlayerInventoryPacket.cs" company="2Dudes">
// Copyright (c) 2018 2Dudes. All rights reserved.
// Author: Jose L. Nunez de Caceres
// jlnunez89@gmail.com
// http://linkedin.com/in/jlnunez89
//
// Licensed under the MIT license.
// See LICENSE.txt file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------

namespace Fibula.Communications.Packets.Outgoing
{
    using Fibula.Communications.Contracts.Abstractions;
    using Fibula.Communications.Contracts.Enumerations;
    using Fibula.Creatures.Contracts.Abstractions;

    /// <summary>
    /// Class that represents a player's inventory packet.
    /// </summary>
    public class PlayerInventoryPacket : IOutboundPacket
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerInventoryPacket"/> class.
        /// </summary>
        /// <param name="player">The player referenced.</param>
        public PlayerInventoryPacket(IPlayer player)
        {
            this.Player = player;
        }

        /// <summary>
        /// Gets the type of this packet.
        /// </summary>
        public byte PacketType => (byte)OutgoingGamePacketType.InventoryItem;

        /// <summary>
        /// Gets a reference to the player.
        /// </summary>
        public IPlayer Player { get; }
    }
}
