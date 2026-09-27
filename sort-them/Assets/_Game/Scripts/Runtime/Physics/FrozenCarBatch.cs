using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace SortThem
{
    public static class FrozenCarBatch
    {
        const int Slices = 6;
        const float CellSize = 2f;
        const float DissolveFactor = 0.9f;

        static readonly int ProbeR = Shader.PropertyToID("_ProbeR");
        static readonly int ProbeG = Shader.PropertyToID("_ProbeG");
        static readonly int ProbeB = Shader.PropertyToID("_ProbeB");

        [StructLayout(LayoutKind.Sequential)]
        struct Vertex
        {
            public ushort X, Y, Z, W;
            public sbyte NX, NY, NZ, NW;
            public byte R, G, B, A;
            public ushort U, V;
        }

        static readonly VertexAttributeDescriptor[] Layout =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float16, 4),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.SNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.UNorm16, 2)
        };

        class Source
        {
            public Vector3[] V, N;
            public ushort[] U, V2;
            public int[] T;
        }

        class Member
        {
            public CarInstance Car;
            public Cell Cell;
            public Vector3 Position;
            public Vector4 R, G, B;
        }

        class Cell
        {
            public Vector3 Center;
            public readonly List<Member> Members = new List<Member>();
            public GameObject Go;
            public MeshRenderer Renderer;
            public Mesh Mesh;
            public bool Dirty = true;
            public bool Chunked;
        }

        public static bool Allowed = true;
        public static float DistanceOverride = -1f;
        public static bool Active { get; private set; }
        public static int Count => _members.Count;
        public static int ChunkedCells { get; private set; }

        static readonly Dictionary<CarInstance, Member> _members = new Dictionary<CarInstance, Member>();
        static readonly Dictionary<long, Cell> _cells = new Dictionary<long, Cell>();
        static readonly List<Cell> _cellList = new List<Cell>();
        static readonly Dictionary<Mesh, Source> _sources = new Dictionary<Mesh, Source>();
        static Vertex[] _verts = new Vertex[0];
        static int[] _tris = new int[0];
        static ushort[] _tris16 = new ushort[0];
        static MaterialPropertyBlock _read;
        static Material _carMaterial, _chunkMaterial;
        static Transform _root;
        static float _distance2, _dissolve2;

        public static void Init(List<CarInstance> cars, Shader shader, float distance, Vector3 eye)
        {
            Shutdown();
            if (!Allowed || shader == null || !shader.isSupported) return;
            foreach (var car in cars)
            {
                if (car == null || car.Rend == null) continue;
                _carMaterial = car.Rend.sharedMaterial;
                if (_carMaterial != null) break;
            }
            if (_carMaterial == null) return;
            _chunkMaterial = new Material(shader) { name = "CarChunk" };
            _chunkMaterial.SetTexture("_BaseMap", _carMaterial.GetTexture("_BaseMap"));
            _chunkMaterial.SetTextureScale("_BaseMap", _carMaterial.GetTextureScale("_BaseMap"));
            _chunkMaterial.SetTextureOffset("_BaseMap", _carMaterial.GetTextureOffset("_BaseMap"));
            _chunkMaterial.SetColor("_BaseColor", _carMaterial.GetColor("_BaseColor"));
            _root = new GameObject("[CarChunks]").transform;
            if (DistanceOverride >= 0f) distance = DistanceOverride;
            _distance2 = distance * distance;
            _dissolve2 = _distance2 * DissolveFactor * DissolveFactor;
            Active = true;
            foreach (var car in cars) if (Eligible(car)) Join(car);
            float t = Time.realtimeSinceStartup;
            foreach (var cell in _cellList) if (Wants(cell, eye, false)) { Rebuild(cell); Chunk(cell); }
            Debug.Log("SortThem: car chunks " + _members.Count + " cars, " + _cellList.Count + " cells, " + ChunkedCells + " chunked, build " + ((Time.realtimeSinceStartup - t) * 1000f).ToString("0") + " ms");
        }

        public static void Shutdown()
        {
            ReleaseAll();
            foreach (var cell in _cellList) if (cell.Mesh != null) Object.Destroy(cell.Mesh);
            if (_root != null) Object.Destroy(_root.gameObject);
            if (_chunkMaterial != null) Object.Destroy(_chunkMaterial);
            _root = null;
            _chunkMaterial = null;
            _carMaterial = null;
            _cells.Clear();
            _cellList.Clear();
            _sources.Clear();
            ChunkedCells = 0;
            Active = false;
        }

        public static bool Contains(CarInstance car) =>
            Active && car != null && _members.TryGetValue(car, out var m) && m.Cell.Chunked;

        public static void Release(CarInstance car)
        {
            if (!Active || car == null || !_members.TryGetValue(car, out var m)) return;
            var cell = m.Cell;
            if (cell.Chunked) Dissolve(cell);
            _members.Remove(car);
            cell.Members.Remove(m);
            cell.Dirty = true;
        }

        static void ReleaseAll()
        {
            foreach (var cell in _cellList) if (cell.Chunked) Dissolve(cell);
            foreach (var cell in _cellList) { cell.Members.Clear(); cell.Dirty = true; }
            _members.Clear();
        }

        public static void Tick(List<CarInstance> cars, bool suspended, Vector3 eye, int frame)
        {
            if (!Active) return;
            if (suspended) { if (_members.Count > 0) ReleaseAll(); return; }
            for (int i = frame % Slices; i < cars.Count; i += Slices)
            {
                var car = cars[i];
                if (car == null) continue;
                if (_members.TryGetValue(car, out var m))
                {
                    if (!StillFrozen(car) || car.transform.position != m.Position) { Release(car); continue; }
                    if (m.Cell.Chunked && car.Rend.enabled) car.Rend.enabled = false;
                }
                else if (Eligible(car)) Join(car);
            }
            bool rebuilt = false;
            foreach (var cell in _cellList)
            {
                bool want = Wants(cell, eye, cell.Chunked);
                if (cell.Chunked)
                {
                    if (!want || cell.Dirty) Dissolve(cell);
                    continue;
                }
                if (!want || cell.Members.Count == 0) continue;
                if (cell.Dirty)
                {
                    if (rebuilt) continue;
                    Rebuild(cell);
                    rebuilt = true;
                }
                Chunk(cell);
            }
        }

        static bool Wants(Cell cell, Vector3 eye, bool chunked)
        {
            var d = cell.Center - eye;
            d.y = 0f;
            return d.sqrMagnitude > (chunked ? _dissolve2 : _distance2);
        }

        static bool StillFrozen(CarInstance car) =>
            car.State == CarState.Loose && !car.Levitating && !car.Hidden && car.gameObject.activeInHierarchy && car.Body.isKinematic;

        static bool Eligible(CarInstance car)
        {
            if (car == null || !StillFrozen(car)) return false;
            var r = car.Rend;
            return r != null && r.enabled && r.sharedMaterial == _carMaterial && car.Lods != null && car.Lods.Length > 0 && car.Lods[car.Lods.Length - 1] != null;
        }

        static void Join(CarInstance car)
        {
            var r = car.Rend;
            CarProbeLight.Apply(car);
            _read ??= new MaterialPropertyBlock();
            r.GetPropertyBlock(_read);
            var m = new Member
            {
                Car = car,
                Position = car.transform.position,
                R = _read.GetVector(ProbeR),
                G = _read.GetVector(ProbeG),
                B = _read.GetVector(ProbeB)
            };
            var cell = GetCell(m.Position);
            m.Cell = cell;
            cell.Members.Add(m);
            cell.Dirty = true;
            _members[car] = m;
        }

        static Cell GetCell(Vector3 p)
        {
            int ix = Mathf.FloorToInt(p.x / CellSize), iz = Mathf.FloorToInt(p.z / CellSize);
            long key = ((long)ix << 32) ^ (uint)iz;
            if (!_cells.TryGetValue(key, out var cell))
            {
                cell = new Cell { Center = new Vector3((ix + 0.5f) * CellSize, p.y, (iz + 0.5f) * CellSize) };
                _cells[key] = cell;
                _cellList.Add(cell);
            }
            return cell;
        }

        static void Chunk(Cell cell)
        {
            if (cell.Mesh == null || cell.Dirty) return;
            cell.Renderer.enabled = true;
            foreach (var m in cell.Members) m.Car.Rend.enabled = false;
            cell.Chunked = true;
            ChunkedCells++;
        }

        static void Dissolve(Cell cell)
        {
            foreach (var m in cell.Members)
            {
                var r = m.Car != null ? m.Car.Rend : null;
                if (r != null) r.enabled = !m.Car.Hidden;
            }
            if (cell.Renderer != null) cell.Renderer.enabled = false;
            cell.Chunked = false;
            ChunkedCells--;
        }

        static void Rebuild(Cell cell)
        {
            cell.Dirty = false;
            int vc = 0, tc = 0;
            foreach (var m in cell.Members)
            {
                var src = GetSource(m.Car.Lods[m.Car.Lods.Length - 1]);
                if (src == null) continue;
                vc += src.V.Length;
                tc += src.T.Length;
            }
            if (_verts.Length < vc) _verts = new Vertex[Mathf.NextPowerOfTwo(vc)];
            if (_tris.Length < tc) _tris = new int[Mathf.NextPowerOfTwo(tc)];
            var origin = cell.Center;
            float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
            int v = 0, t = 0;
            foreach (var m in cell.Members)
            {
                var car = m.Car;
                var src = GetSource(car.Lods[car.Lods.Length - 1]);
                if (src == null) continue;
                var mat = car.Rend.transform.localToWorldMatrix;
                float m00 = mat.m00, m01 = mat.m01, m02 = mat.m02, m03 = mat.m03 - origin.x;
                float m10 = mat.m10, m11 = mat.m11, m12 = mat.m12, m13 = mat.m13 - origin.y;
                float m20 = mat.m20, m21 = mat.m21, m22 = mat.m22, m23 = mat.m23 - origin.z;
                var R = m.R; var G = m.G; var B = m.B;
                int baseIndex = v;
                var sv = src.V; var sn = src.N; var su = src.U; var sv2 = src.V2;
                for (int i = 0; i < sv.Length; i++, v++)
                {
                    var a = sv[i];
                    float px = m00 * a.x + m01 * a.y + m02 * a.z + m03;
                    float py = m10 * a.x + m11 * a.y + m12 * a.z + m13;
                    float pz = m20 * a.x + m21 * a.y + m22 * a.z + m23;
                    var b = sn[i];
                    float nx = m00 * b.x + m01 * b.y + m02 * b.z;
                    float ny = m10 * b.x + m11 * b.y + m12 * b.z;
                    float nz = m20 * b.x + m21 * b.y + m22 * b.z;
                    if (px < minX) minX = px; if (px > maxX) maxX = px;
                    if (py < minY) minY = py; if (py > maxY) maxY = py;
                    if (pz < minZ) minZ = pz; if (pz > maxZ) maxZ = pz;
                    ref var o = ref _verts[v];
                    o.X = Half(px); o.Y = Half(py); o.Z = Half(pz); o.W = 0x3C00;
                    o.NX = SNorm(nx); o.NY = SNorm(ny); o.NZ = SNorm(nz); o.NW = 0;
                    o.R = Ambient(R.x * nx + R.y * ny + R.z * nz + R.w);
                    o.G = Ambient(G.x * nx + G.y * ny + G.z * nz + G.w);
                    o.B = Ambient(B.x * nx + B.y * ny + B.z * nz + B.w);
                    o.A = 255;
                    o.U = su[i]; o.V = sv2[i];
                }
                var st = src.T;
                for (int i = 0; i < st.Length; i++) _tris[t++] = baseIndex + st[i];
            }
            var min = new Vector3(minX, minY, minZ);
            var max = new Vector3(maxX, maxY, maxZ);
            if (cell.Go == null)
            {
                cell.Go = new GameObject("Chunk");
                cell.Go.layer = Layers.LooseItems;
                cell.Go.transform.SetParent(_root, false);
                cell.Go.transform.position = origin;
                cell.Go.AddComponent<MeshFilter>();
                cell.Renderer = cell.Go.AddComponent<MeshRenderer>();
                cell.Renderer.sharedMaterial = _chunkMaterial;
                cell.Renderer.shadowCastingMode = ShadowCastingMode.Off;
                cell.Renderer.receiveShadows = false;
                cell.Renderer.lightProbeUsage = LightProbeUsage.Off;
                cell.Renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                cell.Renderer.enabled = false;
            }
            if (cell.Mesh != null) Object.Destroy(cell.Mesh);
            cell.Mesh = null;
            if (vc == 0) return;
            var mesh = new Mesh { name = "CarChunk" };
            var flags = MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers;
            mesh.SetVertexBufferParams(vc, Layout);
            mesh.SetVertexBufferData(_verts, 0, 0, vc, 0, flags);
            mesh.SetIndexBufferParams(tc, vc > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16);
            if (vc > 65535) mesh.SetIndexBufferData(_tris, 0, 0, tc, flags);
            else
            {
                if (_tris16.Length < tc) _tris16 = new ushort[Mathf.NextPowerOfTwo(tc)];
                for (int i = 0; i < tc; i++) _tris16[i] = (ushort)_tris[i];
                mesh.SetIndexBufferData(_tris16, 0, 0, tc, flags);
            }
            var bounds = new Bounds((min + max) * 0.5f, max - min);
            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, tc) { bounds = bounds, vertexCount = vc }, flags);
            mesh.bounds = bounds;
            mesh.UploadMeshData(true);
            cell.Mesh = mesh;
            cell.Go.GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        static Source GetSource(Mesh mesh)
        {
            if (mesh == null) return null;
            if (_sources.TryGetValue(mesh, out var s)) return s;
            if (!mesh.isReadable) { _sources[mesh] = null; return null; }
            var uv = mesh.uv;
            s = new Source { V = mesh.vertices, N = mesh.normals, T = mesh.triangles };
            if (s.N.Length != s.V.Length || uv.Length != s.V.Length) s = null;
            else
            {
                s.U = new ushort[uv.Length];
                s.V2 = new ushort[uv.Length];
                for (int i = 0; i < uv.Length; i++) { s.U[i] = UNorm16(uv[i].x); s.V2[i] = UNorm16(uv[i].y); }
            }
            _sources[mesh] = s;
            return s;
        }

        static sbyte SNorm(float v) => (sbyte)Mathf.Clamp(Mathf.RoundToInt(v * 127f), -127, 127);

        static ushort UNorm16(float v) => (ushort)Mathf.Clamp(Mathf.RoundToInt(v * 65535f), 0, 65535);

        static byte Ambient(float v) => v <= 0f ? (byte)0 : v >= 2f ? (byte)255 : (byte)(v * 127.5f + 0.5f);

        static ushort Half(float f)
        {
            uint x = (uint)System.BitConverter.SingleToInt32Bits(f);
            uint sign = (x >> 16) & 0x8000u;
            int exp = (int)((x >> 23) & 0xFF) - 127 + 15;
            uint mant = x & 0x7FFFFFu;
            if (exp <= 0)
            {
                if (exp < -10) return (ushort)sign;
                mant |= 0x800000u;
                int shift = 14 - exp;
                uint h = mant >> shift;
                if (((mant >> (shift - 1)) & 1u) != 0) h++;
                return (ushort)(sign | h);
            }
            if (exp >= 31) return (ushort)(sign | 0x7C00u);
            uint r = sign | ((uint)exp << 10) | (mant >> 13);
            if ((mant & 0x1000u) != 0) r++;
            return (ushort)r;
        }
    }
}
