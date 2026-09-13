using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace SortThem.Editor
{
    public static class ProfilerDump
    {
        const int Frames = 600;

        struct Row { public float Frame, Wait, Render, Physics, Scripts, Sort, Draw, Cull; public int Steps; }

        [MenuItem("SortThem/Dev/Profiler Summary (last 600 frames)")]
        public static void Summary()
        {
            int last = ProfilerDriver.lastFrameIndex;
            if (last < 0) { Debug.Log("SortThem: profiler has no frames (open Window/Analysis/Profiler, connect a player)"); return; }
            int first = Mathf.Max(ProfilerDriver.firstFrameIndex, last - Frames);
            int cT = HierarchyFrameDataView.columnTotalTime;
            var rows = new List<Row>();
            for (int f = first; f <= last; f++)
            {
                var v = ProfilerDriver.GetHierarchyFrameDataView(f, 0, HierarchyFrameDataView.ViewModes.Default, cT, false);
                if (v == null || !v.valid) continue;
                var r = new Row { Frame = v.frameTimeMs };
                var top = new List<int>(); v.GetItemChildren(v.GetRootItemID(), top);
                foreach (var k in top)
                {
                    var kk = new List<int>(); v.GetItemChildren(k, kk);
                    foreach (var c in kk)
                    {
                        string n = v.GetItemName(c); float t = v.GetItemColumnDataAsFloat(c, cT);
                        if (n == "WaitForTargetFPS") r.Wait += t;
                        else if (n == "PostLateUpdate.FinishFrameRendering") { r.Render += t; Walk(v, c, 0, cT, ref r); }
                        else if (n == "FixedUpdate.PhysicsFixedUpdate") { r.Physics += t; r.Steps++; }
                        else if (n == "Update.ScriptRunBehaviourUpdate") r.Scripts += t;
                    }
                }
                rows.Add(r); v.Dispose();
            }
            if (rows.Count == 0) { Debug.Log("SortThem: no valid frames"); return; }
            string conn = ProfilerDriver.profileEditor ? "Editor" : ProfilerDriver.connectedProfiler == -1 ? "player (disconnected, last data)" : ProfilerDriver.GetConnectionIdentifier(ProfilerDriver.connectedProfiler);
            var sb = new System.Text.StringBuilder($"SortThem profiler summary: player={conn} frames {first}-{last} n={rows.Count}\n");
            Report(sb, "all", rows);
            Report(sb, "calm (physics < 3 ms)", rows.Where(r => r.Physics < 3f).ToList());
            Report(sb, "active (physics >= 3 ms)", rows.Where(r => r.Physics >= 3f).ToList());
            sb.Append("worst: " + string.Join(" | ", rows.OrderByDescending(r => r.Frame).Take(6).Select(r => $"{r.Frame:0}ms render={r.Render:0} physics={r.Physics:0}x{r.Steps} scripts={r.Scripts:0}")));
            Debug.Log(sb.ToString());
        }

        static void Walk(HierarchyFrameDataView v, int id, int depth, int cT, ref Row r)
        {
            string n = v.GetItemName(id); float t = v.GetItemColumnDataAsFloat(id, cT);
            if (n == "RenderLoop.Sort") { r.Sort += t; return; }
            if (n == "RenderLoop.ScheduleDraw") { r.Draw += t; return; }
            if (n == "CullScriptable") { r.Cull += t; return; }
            if (depth > 9) return;
            var kids = new List<int>(); v.GetItemChildren(id, kids);
            foreach (var c in kids) Walk(v, c, depth + 1, cT, ref r);
        }

        static float P(List<Row> l, System.Func<Row, float> sel, float q)
        {
            var s = l.Select(sel).OrderBy(x => x).ToList();
            return s[Mathf.Clamp((int)(s.Count * q), 0, s.Count - 1)];
        }

        static void Report(System.Text.StringBuilder sb, string name, List<Row> l)
        {
            sb.Append($"== {name}: n={l.Count}\n");
            if (l.Count == 0) return;
            sb.Append($"  frame p50={P(l, r => r.Frame, .5f):0.0} p90={P(l, r => r.Frame, .9f):0.0} max={l.Max(r => r.Frame):0.0} ms | wait(vsync) p50={P(l, r => r.Wait, .5f):0.0}\n");
            sb.Append($"  render p50={P(l, r => r.Render, .5f):0.0} (draw {P(l, r => r.Draw, .5f):0.0}, sort {P(l, r => r.Sort, .5f):0.0}, cull {P(l, r => r.Cull, .5f):0.0})\n");
            sb.Append($"  physics p50={P(l, r => r.Physics, .5f):0.0} p90={P(l, r => r.Physics, .9f):0.0} max={l.Max(r => r.Physics):0.0} | steps/frame p50={P(l, r => r.Steps, .5f):0} max={l.Max(r => r.Steps)}\n");
            sb.Append($"  scripts p50={P(l, r => r.Scripts, .5f):0.0} max={l.Max(r => r.Scripts):0.0}\n");
        }
    }
}
