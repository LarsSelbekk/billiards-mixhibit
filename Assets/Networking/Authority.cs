namespace Networking
{
    /**
     * Based on fbsamples / oculus-networked-physics-sample
     *
     *      Copyright (c) 2017-present, Facebook, Inc.
     *      All rights reserved.
     *
     *      This source code is licensed under the BSD-style license found in the
     *      LICENSE file in the Scripts directory of this source tree. An additional grant
     *      of patent rights can be found in the PATENTS file in the same directory.
     *
     */
    public struct Authority
    {
        public const ulong NoClientID = ulong.MaxValue;

        /*
         *  This function determines when we should apply state updates to objects.
         *  It is designed to allow clients to pre-emptively take authority over objects when
         *  they grab and interact with them indirectly (eg. throwing objects at other objects).
         *  In short, ownership sequence increases each time a player grabs an object, and authority
         *  sequence increases each time an object is touched by an object under authority of that player.
         *  When a client sees an object under its authority has come to rest, it returns that object to
         *  default authority and commits its result back to the server. The logic below implements
         *  this direction of flow, as well as resolving conflicts when two clients think they both
         *  own the same object, or have interacted with the same object. The first player to interact,
         *  from the point of view of the server, wins.
         */
        public static bool ShouldApplyObjectStateUpdate(
            ushort localOwnershipSequence,
            ushort localAuthoritySequence,
            ushort ownershipSequence,
            ushort authoritySequence,
            ulong authorityClientId,
            ulong localAuthorityClientId,
            ulong fromClientId,
            ulong toClientId,
            ulong serverClientId
        )
        {
            // *** OWNERSHIP SEQUENCE ***

            // Must accept if ownership sequence is newer
            if (NetworkUtils.SequenceGreaterThan(ownershipSequence, localOwnershipSequence))
            {
                return true;
            }

            // Must reject if ownership sequence is older
            if (NetworkUtils.SequenceLessThan(ownershipSequence, localOwnershipSequence))
            {
                return false;
            }

            // *** AUTHORITY SEQUENCE ***

            // accept if the authority sequence is newer
            if (NetworkUtils.SequenceGreaterThan(authoritySequence, localAuthoritySequence))
            {
                return true;
            }

            // reject if the authority sequence is older
            if (NetworkUtils.SequenceLessThan(authoritySequence, localAuthoritySequence))
            {
                return false;
            }

            // Both sequence numbers are the same. Resolve authority conflicts!
            if (fromClientId == serverClientId)
            {
                // =============================
                //       server -> client
                // =============================

                // ignore if the server says the cube is under authority of this client. the server is just confirming we have authority
                if (authorityClientId == toClientId)
                {
                    return false;
                }

                // accept if the server says the cube is under authority of another client
                if (authorityClientId != NoClientID && authorityClientId != toClientId)
                {
                    return true;
                }

                // ignore if the server says the cube is default authority, but the client has already taken authority over the cube
                if (authorityClientId == NoClientID && localAuthorityClientId == toClientId)
                {
                    return false;
                }

                // accept if the server says the cube is default authority, and on the client it is also default authority
                if (authorityClientId == NoClientID && localAuthorityClientId == NoClientID)
                {
                    return true;
                }
            }
            else
            {
                // =============================
                //       client -> server
                // =============================

                // reject if the cube is not under authority of the client
                if (authorityClientId != fromClientId)
                {
                    return false;
                }

                // accept if the cube is under authority of this client
                if (localAuthorityClientId == fromClientId)
                {
                    return true;
                }
            }

            // otherwise, reject.
            return false;
        }
    }
}