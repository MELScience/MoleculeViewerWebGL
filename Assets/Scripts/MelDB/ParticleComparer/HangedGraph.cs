using System.Collections.Generic;

namespace ParticleComparerInternal
{
    public class HangedGraph
    {
        public AtomNodeProto head;
        public ParticleGraphProto proto;
        public int[] nodeLayers;

        private HashSet<AtomNodeProto> processed;
        Queue<AtomNodeProto> queue;
        private static uint[] _layerHashes = new uint[ParticleComparer.MAX_ATOMS]; // to avoid allocations
        private int layers;

        public HangedGraph()
        {
            nodeLayers = new int[ParticleComparer.MAX_ATOMS];
            queue = new Queue<AtomNodeProto>(ParticleComparer.MAX_ATOMS);
            processed = new HashSet<AtomNodeProto>();
        }

        public void AttachTo(ParticleGraphProto proto)
        {
            this.proto = proto;
        }

        public bool AllNodesConnected(AtomNodeProto head)
        {
            processed.Clear();
            queue.Clear();

            queue.Enqueue(head);
            processed.Add(head);

            while (queue.Count > 0)
            {
                for (int i = queue.Count; i > 0; i--)
                {
                    var a = queue.Dequeue();
                    var bonds = a.bonds;
                    for (int bi = bonds.Count - 1; bi >= 0; bi--)
                    {
                        var b = bonds[bi];
                        var other = b.Other(a);
                        if (!processed.Contains(other))
                        {
                            queue.Enqueue(other);
                            processed.Add(other);
                        }
                    }
                }
            }
            return proto.atoms.Count == processed.Count;
        }

        public void Hang(AtomNodeProto head)
        {
            this.head = head;

            processed.Clear();
            queue.Clear();

            queue.Enqueue(head);
            processed.Add(head);
            layers = 0;

            while (queue.Count > 0)
            {
                for (int i = queue.Count; i > 0; i--)
                {
                    var a = queue.Dequeue();
                    nodeLayers[a.originalIndx] = layers;
                    var bonds = a.bonds;
                    for (int bi = bonds.Count - 1; bi >= 0; bi--)
                    {
                        var b = bonds[bi];
                        var other = b.Other(a);
                        if (!processed.Contains(other))
                        {
                            queue.Enqueue(other);
                            processed.Add(other);
                        }
                    }
                }
                layers++;
            }
        }

        public uint GetHash(uint[] hashes)
        {
            uint hash = head.protoHash;
            
            for (int i = 0; i < layers; i++)
                _layerHashes[i] = hashes[i];
            var atoms = proto.atoms;
            for (int i = atoms.Count - 1; i >= 0; i--)
            {
                var atom = atoms[i];
                int layer = nodeLayers[atom.originalIndx];
                uint layerHash = hashes[layer];
                uint nh = unchecked(layerHash * 179424691 + atom.protoHash * 179426081);
                _layerHashes[layer] = unchecked(_layerHashes[layer] + nh);
            }
            for (int i = 0; i < layers; i++)
                hash = unchecked(hash * 104917 + _layerHashes[i]);

            var bonds = proto.bonds;
            for (int i = bonds.Count - 1; i >= 0; i--)
            {
                var bond = bonds[i];
                var atom1 = bond.node1;
                var atom2 = bond.node2;
                uint layer1 = hashes[nodeLayers[atom1.originalIndx]];
                uint layer2 = hashes[nodeLayers[atom2.originalIndx]];
                uint nh1 = unchecked(layer1 * 179424691 + atom1.protoHash);
                uint nh2 = unchecked(layer2 * 179424691 + atom2.protoHash);

                nh1 ^= nh2; // hash from atoms
                nh1 = unchecked(nh1 * 179426081 + hashes[bond.type]); // hash from type

                hash = unchecked(hash + nh1);
            }
            return hash;
        }
    }
}