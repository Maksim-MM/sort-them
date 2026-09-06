using System;
using System.IO;
using UnityEngine;

namespace SortThem
{
    public class SaveService
    {
        const int Magic = 0x53545331;
        const int Version = 1;
        const float PosScale = 200f;

        readonly GameManager _gm;
        readonly ISaveStorage _storage;
        float _timer;

        public event Action<string> Saved;
        public DateTime LastSaveTime { get; private set; }

        public SaveService(GameManager gm, ISaveStorage storage)
        {
            _gm = gm;
            _storage = storage;
        }

        public void Tick(float dt)
        {
            _timer += dt;
            if (_timer >= _gm.Config.AutosaveInterval)
            {
                _timer = 0f;
                SaveNow("autosave");
            }
        }

        public void SaveNow(string reason)
        {
            if (!_gm.Ready) return;
            try
            {
                _storage.Save(Serialize());
                _timer = 0f;
                LastSaveTime = DateTime.Now;
                Saved?.Invoke(reason);
            }
            catch (Exception e)
            {
                Debug.LogError("SortThem: save failed: " + e);
            }
        }

        public void ClearSave() => _storage.Clear();

        public bool Load()
        {
            if (!_storage.TryLoad(out var data)) return false;
            try
            {
                return Deserialize(data);
            }
            catch (Exception e)
            {
                Debug.LogError("SortThem: load failed: " + e);
                return false;
            }
        }

        byte[] Serialize()
        {
            using var ms = new MemoryStream(_gm.Cars.Count * 12 + 256);
            using var w = new BinaryWriter(ms);
            w.Write(Magic);
            w.Write(Version);
            w.Write(_gm.Economy.Balance);
            w.Write(_gm.CollectiblesFound);

            var upgrades = _gm.Upgrades.All;
            w.Write((byte)upgrades.Count);
            foreach (var u in upgrades)
            {
                w.Write((byte)u.Kind);
                w.Write((byte)_gm.Upgrades.Level(u));
            }

            var inv = _gm.Inventory;
            w.Write((byte)(inv != null ? inv.Items.Count : 0));
            if (inv != null) foreach (var car in inv.Items) w.Write((ushort)car.InstanceId);
            w.Write((byte)(inv != null ? inv.ActiveIndex : 0));

            w.Write(_gm.Cars.Count);
            foreach (var car in _gm.Cars)
            {
                w.Write((byte)car.State);
                switch (car.State)
                {
                    case CarState.Placed:
                        w.Write((ushort)(car.Shelf != null ? car.Shelf.ShelfId : 0));
                        w.Write((byte)car.SlotIndex);
                        break;
                    case CarState.Loose:
                        var p = car.transform.position;
                        var q = car.transform.rotation;
                        w.Write(Q16(p.x));
                        w.Write(Q16(p.y));
                        w.Write(Q16(p.z));
                        w.Write(Q8(q.x));
                        w.Write(Q8(q.y));
                        w.Write(Q8(q.z));
                        w.Write(Q8(q.w));
                        break;
                }
            }
            w.Flush();
            return ms.ToArray();
        }

        bool Deserialize(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var r = new BinaryReader(ms);
            if (r.ReadInt32() != Magic) return false;
            int version = r.ReadInt32();
            if (version != Version) return false;
            float balance = r.ReadSingle();
            int collectibles = r.ReadInt32();

            int upgradeCount = r.ReadByte();
            for (int i = 0; i < upgradeCount; i++)
            {
                var kind = (UpgradeKind)r.ReadByte();
                int level = r.ReadByte();
                _gm.Upgrades.SetLevel(kind, level);
            }

            int invCount = r.ReadByte();
            var invIds = new int[invCount];
            for (int i = 0; i < invCount; i++) invIds[i] = r.ReadUInt16();
            int activeIndex = r.ReadByte();

            int carCount = r.ReadInt32();
            if (carCount != _gm.Cars.Count)
            {
                Debug.LogWarning("SortThem: save car count mismatch, ignoring save");
                return false;
            }

            var shelves = _gm.Shelves;
            for (int i = 0; i < carCount; i++)
            {
                var car = _gm.Cars[i];
                var state = (CarState)r.ReadByte();
                switch (state)
                {
                    case CarState.Placed:
                    {
                        int shelfId = r.ReadUInt16();
                        int slot = r.ReadByte();
                        if (shelfId >= 0 && shelfId < shelves.Count)
                        {
                            var shelf = shelves[shelfId];
                            if (!shelf.TryPlace(car, slot, false))
                            {
                                int free = shelf.Accepts(car.Data) ? shelf.FirstFreeSlot() : -1;
                                if (free < 0 || !shelf.TryPlace(car, free, false)) Unstick(car);
                            }
                        }
                        else Unstick(car);
                        break;
                    }
                    case CarState.Loose:
                    {
                        var p = new Vector3(D16(r.ReadInt16()), D16(r.ReadInt16()), D16(r.ReadInt16()));
                        var q = new Quaternion(D8(r.ReadSByte()), D8(r.ReadSByte()), D8(r.ReadSByte()), D8(r.ReadSByte()));
                        if (q.x == 0f && q.y == 0f && q.z == 0f && q.w == 0f) q = Quaternion.identity;
                        q.Normalize();
                        car.SetLoose(p, q, true);
                        break;
                    }
                    case CarState.Held:
                        car.SetHeld();
                        break;
                }
            }

            var inv = _gm.Inventory;
            if (inv != null)
            {
                inv.Items.Clear();
                foreach (var id in invIds)
                {
                    if (id < 0 || id >= _gm.Cars.Count) continue;
                    var car = _gm.Cars[id];
                    if (inv.Items.Count < inv.Capacity)
                    {
                        car.SetHeld();
                        inv.Items.Add(car);
                    }
                    else Unstick(car);
                }
                inv.ActiveIndex = Mathf.Clamp(activeIndex, 0, Mathf.Max(0, inv.Items.Count - 1));
                inv.NotifyChanged();
            }
            foreach (var car in _gm.Cars)
                if (car.State == CarState.Held && (inv == null || !inv.Items.Contains(car))) Unstick(car);

            _gm.Economy.SetBalance(balance);
            _gm.UnstuckCars();
            return true;
        }

        void Unstick(CarInstance car)
        {
            var c = _gm.Config.UnstuckCenter;
            car.SetLoose(c + UnityEngine.Random.insideUnitSphere, UnityEngine.Random.rotation, false);
        }

        static short Q16(float v) => (short)Mathf.Clamp(Mathf.RoundToInt(v * PosScale), short.MinValue, short.MaxValue);
        static float D16(short v) => v / PosScale;
        static sbyte Q8(float v) => (sbyte)Mathf.Clamp(Mathf.RoundToInt(v * 127f), -127, 127);
        static float D8(sbyte v) => v / 127f;
    }
}
