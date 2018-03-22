using System.Collections.Generic;

namespace ParticleComparerInternal
{
    public class ParticleGraphProto
    {
        public List<AtomNodeProto> atoms;
        public List<NodeBondProto> bonds;
        private uint[] _hashes;

        public ParticleGraphProto(ParticleInfo particle, bool exact, uint[] hashes)
        {
            atoms = new List<AtomNodeProto>();
            _hashes = hashes;
            var atms = particle.atoms;
            for (int i = 0; i < atms.Count; i++)
                atoms.Add(new AtomNodeProto(atms[i].element, i, hashes));
            InitBonds(particle.bonds, exact);
        }
        public ParticleGraphProto(List<Element> atms, List<BondInfo> bonds, bool exact, uint[] hashes)
        {
            atoms = new List<AtomNodeProto>();
            _hashes = hashes;
            for (int i = 0; i < atms.Count; i++)
                atoms.Add(new AtomNodeProto(atms[i], i, hashes));
            InitBonds(bonds, exact);
        }

        private void InitBonds(List<BondInfo> bnds, bool exact)
        {
            bonds = new List<NodeBondProto>();
            for (int i = 0; i < bnds.Count; i++)
            {
                var b = bnds[i];
                var a1 = atoms[b.atom1];
                var a2 = atoms[b.atom2];
                var bond = new NodeBondProto(a1, a2, exact ? b.bondType : BondInfo.BondType.UNKNOWN, i);
                a1.bonds.Add(bond);
                a2.bonds.Add(bond);
                bonds.Add(bond);
            }
        }

        public void PrepareSimpleHashes()
        {
            for (int i = 0; i < atoms.Count; i++)
            {
                var a = atoms[i];
                a.PrepareSimpleHash(_hashes);
            }
        }

        public bool TrySortAndUpdateHash(out uint currentHash)
        {
            // sort
            for (int i = atoms.Count - 1; i >= 0; i--)
            {
                var iAtom = atoms[i];
                var lowestHash = iAtom.protoHash;
                int lower = i;
                for (int j = i - 1; j >= 0; j--)
                {
                    if (atoms[j].protoHash < lowestHash)
                    {
                        lower = j;
                        lowestHash = atoms[j].protoHash;
                    }
                }
                atoms[i] = atoms[lower];
                atoms[lower] = iAtom;
            }
            // check unique and calc hash
            bool uniqueAtoms = true;
            currentHash = 0;
            var lastHash = uint.MaxValue;
            for (int i = atoms.Count - 1; i >= 0; i--)
            {
                var hash = atoms[i].protoHash;
                currentHash = currentHash * 179425027 + hash;
                if (atoms[i].bonds.Count == 1)
                    continue;
                uniqueAtoms &= hash != lastHash;
                lastHash = hash;
            }
            return uniqueAtoms;
        }

        public int CountSimilarVariants(out uint hash)
        {
            int v = 1;
            var last = uint.MaxValue;
            for (int ii = atoms.Count - 1; ii >= 0; ii--)
            {
                if (atoms[ii].bonds.Count == 1)
                    continue;

                var n = atoms[ii].protoHash;
                if (last == n)
                {
                    v++;
                    while (ii > 0 && atoms[--ii].protoHash == last)
                        if (atoms[ii].bonds.Count > 1)
                            v++;
                    hash = last;
                    return v;
                }
                last = n;
            }
            throw new System.Exception();
        }

        public void ModifySimilarHash(uint targetHash, int variant)
        {
            for (int ii = atoms.Count - 1; ii >= 0; ii--)
            {
                if (atoms[ii].bonds.Count == 1)
                    continue;

                var n = atoms[ii].protoHash;
                if (targetHash == n)
                {
                    if (variant-- <= 0)
                    {
                        atoms[ii].protoHash = ~n;
                        return;
                    }
                }
            }
            throw new System.Exception();
        }
    }

    public class AtomNodeProto
    {
        public uint protoHash;
        public int type;
        public List<NodeBondProto> bonds = new List<NodeBondProto>(6);
        public int originalIndx;

        public AtomNodeProto(Element type, int indx, uint[] hashes)
        {
            this.type = (int)type;
            originalIndx = indx;
            protoHash = hashes[this.type];
        }

        public void PrepareSimpleHash(uint[] hashes)
        {
            for (int i = bonds.Count - 1; i >= 0; i--)
            {
                var b = bonds[i];
                uint h = hashes[b.type];
                h = unchecked(h * 179426549 + hashes[b.Other(this).type]);
                protoHash = unchecked(protoHash + h);
            }
        }
    }

    public class NodeBondProto
    {
        public int type;
        public AtomNodeProto node1;
        public AtomNodeProto node2;
        public int originalIndx;

        public NodeBondProto(AtomNodeProto a1, AtomNodeProto a2, BondInfo.BondType type, int indx)
        {
            originalIndx = indx;
            this.type = 128 + (int)type;
            node1 = a1;
            node2 = a2;
        }

        public AtomNodeProto Other(AtomNodeProto node)
        {
            return node1 == node ? node2 : node1;
        }
    }
}