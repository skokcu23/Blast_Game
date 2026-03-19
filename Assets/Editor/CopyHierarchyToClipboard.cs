// DeepHierarchyExport.cs
// Place inside any Assets/Editor/ folder.
// Menu: Claude ▸ Export Full Hierarchy to Clipboard

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DeepHierarchyExport
{
    // ── Types we skip entirely (noise / circular refs) ──────────────────────
    static readonly HashSet<Type> SkipTypes = new HashSet<Type>
    {
        typeof(Transform),          // printed separately
        typeof(HideFlags),
    };

    // ── Field/property names that tend to be huge or useless ────────────────
    static readonly HashSet<string> SkipNames = new HashSet<string>
    {
        "mesh", "sharedMesh", "material", "sharedMaterial",
        "materials", "sharedMaterials",
        "sprite", "texture", "sprite", "font",
        "clip", "clips", "animationClips",
        "hideFlags", "name", "tag",
    };

    [MenuItem("Claude/Export Full Hierarchy to Clipboard")]
    public static void Export()
    {
        var sb = new StringBuilder();
        var scene = SceneManager.GetActiveScene();

        sb.AppendLine("=== UNITY SCENE: " + scene.name + " ===");
        sb.AppendLine();

        foreach (var root in scene.GetRootGameObjects())
            WriteGameObject(sb, root, 0);

        string output = sb.ToString();
        GUIUtility.systemCopyBuffer = output;
        Debug.Log("[Claude] Full hierarchy copied (" + output.Length + " chars)");
        EditorUtility.DisplayDialog("Done",
            $"Full hierarchy copied to clipboard ({output.Length} characters).\nPaste it into Claude!", "OK");
    }

    // ── GameObject ───────────────────────────────────────────────────────────
    static void WriteGameObject(StringBuilder sb, GameObject go, int depth)
    {
        string pad  = Pad(depth);
        string pad1 = Pad(depth + 1);
        string pad2 = Pad(depth + 2);

        // Header line
        sb.AppendLine($"{pad}[{go.name}]  active={go.activeSelf}  tag={go.tag}  layer={LayerMask.LayerToName(go.layer)}");

        // Transform (always first)
        var t = go.transform;
        sb.AppendLine($"{pad1}Transform:");
        sb.AppendLine($"{pad2}position       = {V3(t.localPosition)}");
        sb.AppendLine($"{pad2}rotation       = {V3(t.localEulerAngles)}");
        sb.AppendLine($"{pad2}scale          = {V3(t.localScale)}");

        // Every other component
        foreach (var comp in go.GetComponents<Component>())
        {
            if (comp == null) continue;
            var type = comp.GetType();
            if (type == typeof(Transform)) continue;

            sb.AppendLine($"{pad1}{type.Name}:");
            WriteComponent(sb, comp, depth + 2);
        }

        // Recurse children
        foreach (Transform child in go.transform)
            WriteGameObject(sb, child.gameObject, depth + 1);
    }

    // ── Component ────────────────────────────────────────────────────────────
    static void WriteComponent(StringBuilder sb, Component comp, int depth)
    {
        string pad = Pad(depth);
        var type   = comp.GetType();
        bool wroteAnything = false;

        // ── Known component types with hand-crafted output ───────────────────
        if (comp is Renderer rend)         { WriteRenderer(sb, rend, pad);    wroteAnything = true; }
        if (comp is Collider col)          { WriteCollider(sb, col, pad);     wroteAnything = true; }
        if (comp is Rigidbody rb)          { WriteRigidbody(sb, rb, pad);     wroteAnything = true; }
        if (comp is Rigidbody2D rb2)       { WriteRigidbody2D(sb, rb2, pad);  wroteAnything = true; }
        if (comp is Camera cam)            { WriteCamera(sb, cam, pad);       wroteAnything = true; }
        if (comp is Light light)           { WriteLight(sb, light, pad);      wroteAnything = true; }
        if (comp is AudioSource audio)     { WriteAudio(sb, audio, pad);      wroteAnything = true; }
        if (comp is Animator anim)         { WriteAnimator(sb, anim, pad);    wroteAnything = true; }
        if (comp is Canvas canvas)         { WriteCanvas(sb, canvas, pad);    wroteAnything = true; }
        if (comp is UnityEngine.UI.Text txt) { WriteUIText(sb, txt, pad);     wroteAnything = true; }
        if (comp is UnityEngine.UI.Image img){ WriteUIImage(sb, img, pad);    wroteAnything = true; }
        if (comp is UnityEngine.UI.Button btn){ WriteUIButton(sb, btn, pad);  wroteAnything = true; }
        if (comp is ParticleSystem ps)     { WriteParticles(sb, ps, pad);     wroteAnything = true; }
        if (comp is MeshFilter mf && mf.sharedMesh != null)
        { sb.AppendLine($"{pad}mesh           = {mf.sharedMesh.name}"); wroteAnything = true; }

        // ── Reflection fallback for everything else (MonoBehaviours, etc.) ───
        if (!wroteAnything || IsMonoBehaviour(type))
            WriteViaReflection(sb, comp, pad, type, wroteAnything);
    }

    // ── Hand-crafted writers ─────────────────────────────────────────────────

    static void WriteRenderer(StringBuilder sb, Renderer r, string pad)
    {
        sb.AppendLine($"{pad}enabled        = {r.enabled}");
        sb.AppendLine($"{pad}castShadows    = {r.shadowCastingMode}");
        sb.AppendLine($"{pad}receiveShadows = {r.receiveShadows}");
        if (r.sharedMaterials != null)
            for (int i = 0; i < r.sharedMaterials.Length; i++)
                sb.AppendLine($"{pad}material[{i}]    = {(r.sharedMaterials[i] != null ? r.sharedMaterials[i].name : "null")}");
    }

    static void WriteCollider(StringBuilder sb, Collider c, string pad)
    {
        sb.AppendLine($"{pad}enabled        = {c.enabled}");
        sb.AppendLine($"{pad}isTrigger      = {c.isTrigger}");
        sb.AppendLine($"{pad}center(world)  = {V3(c.bounds.center)}");
        sb.AppendLine($"{pad}size(world)    = {V3(c.bounds.size)}");
        if (c is BoxCollider bc)   { sb.AppendLine($"{pad}center(local)  = {V3(bc.center)}"); sb.AppendLine($"{pad}size(local)    = {V3(bc.size)}"); }
        if (c is SphereCollider sc){ sb.AppendLine($"{pad}radius         = {sc.radius}"); }
        if (c is CapsuleCollider cc){ sb.AppendLine($"{pad}radius         = {cc.radius}"); sb.AppendLine($"{pad}height         = {cc.height}"); sb.AppendLine($"{pad}direction      = {cc.direction}"); }
        if (c is MeshCollider mc)  { sb.AppendLine($"{pad}convex         = {mc.convex}"); sb.AppendLine($"{pad}mesh           = {(mc.sharedMesh != null ? mc.sharedMesh.name : "null")}"); }
    }

    static void WriteRigidbody(StringBuilder sb, Rigidbody r, string pad)
    {
        sb.AppendLine($"{pad}mass           = {r.mass}");
        sb.AppendLine($"{pad}drag           = {r.drag}");
        sb.AppendLine($"{pad}angularDrag    = {r.angularDrag}");
        sb.AppendLine($"{pad}useGravity     = {r.useGravity}");
        sb.AppendLine($"{pad}isKinematic    = {r.isKinematic}");
        sb.AppendLine($"{pad}constraints    = {r.constraints}");
        sb.AppendLine($"{pad}interpolation  = {r.interpolation}");
        sb.AppendLine($"{pad}collisionMode  = {r.collisionDetectionMode}");
    }

    static void WriteRigidbody2D(StringBuilder sb, Rigidbody2D r, string pad)
    {
        sb.AppendLine($"{pad}mass           = {r.mass}");
        sb.AppendLine($"{pad}drag           = {r.drag}");
        sb.AppendLine($"{pad}angularDrag    = {r.angularDrag}");
        sb.AppendLine($"{pad}gravityScale   = {r.gravityScale}");
        sb.AppendLine($"{pad}isKinematic    = {r.isKinematic}");
        sb.AppendLine($"{pad}constraints    = {r.constraints}");
        sb.AppendLine($"{pad}bodyType       = {r.bodyType}");
    }

    static void WriteCamera(StringBuilder sb, Camera c, string pad)
    {
        sb.AppendLine($"{pad}clearFlags     = {c.clearFlags}");
        sb.AppendLine($"{pad}background     = {C(c.backgroundColor)}");
        sb.AppendLine($"{pad}cullingMask    = {c.cullingMask}");
        sb.AppendLine($"{pad}projection     = {(c.orthographic ? "Orthographic" : "Perspective")}");
        if (c.orthographic) sb.AppendLine($"{pad}orthoSize      = {c.orthographicSize}");
        else                sb.AppendLine($"{pad}fieldOfView    = {c.fieldOfView}");
        sb.AppendLine($"{pad}nearClip       = {c.nearClipPlane}");
        sb.AppendLine($"{pad}farClip        = {c.farClipPlane}");
        sb.AppendLine($"{pad}depth          = {c.depth}");
        sb.AppendLine($"{pad}viewportRect   = {c.rect}");
    }

    static void WriteLight(StringBuilder sb, Light l, string pad)
    {
        sb.AppendLine($"{pad}type           = {l.type}");
        sb.AppendLine($"{pad}color          = {C(l.color)}");
        sb.AppendLine($"{pad}intensity      = {l.intensity}");
        sb.AppendLine($"{pad}range          = {l.range}");
        sb.AppendLine($"{pad}spotAngle      = {l.spotAngle}");
        sb.AppendLine($"{pad}shadows        = {l.shadows}");
        sb.AppendLine($"{pad}shadowStrength = {l.shadowStrength}");
        sb.AppendLine($"{pad}renderMode     = {l.renderMode}");
    }

    static void WriteAudio(StringBuilder sb, AudioSource a, string pad)
    {
        sb.AppendLine($"{pad}clip           = {(a.clip != null ? a.clip.name : "null")}");
        sb.AppendLine($"{pad}volume         = {a.volume}");
        sb.AppendLine($"{pad}pitch          = {a.pitch}");
        sb.AppendLine($"{pad}loop           = {a.loop}");
        sb.AppendLine($"{pad}playOnAwake    = {a.playOnAwake}");
        sb.AppendLine($"{pad}spatialBlend   = {a.spatialBlend}");
        sb.AppendLine($"{pad}minDistance    = {a.minDistance}");
        sb.AppendLine($"{pad}maxDistance    = {a.maxDistance}");
        sb.AppendLine($"{pad}rolloffMode    = {a.rolloffMode}");
        sb.AppendLine($"{pad}mute           = {a.mute}");
    }

    static void WriteAnimator(StringBuilder sb, Animator a, string pad)
    {
        sb.AppendLine($"{pad}controller     = {(a.runtimeAnimatorController != null ? a.runtimeAnimatorController.name : "null")}");
        sb.AppendLine($"{pad}avatar         = {(a.avatar != null ? a.avatar.name : "null")}");
        sb.AppendLine($"{pad}applyRootMotion= {a.applyRootMotion}");
        sb.AppendLine($"{pad}updateMode     = {a.updateMode}");
        sb.AppendLine($"{pad}cullingMode    = {a.cullingMode}");
    }

    static void WriteCanvas(StringBuilder sb, Canvas c, string pad)
    {
        sb.AppendLine($"{pad}renderMode     = {c.renderMode}");
        sb.AppendLine($"{pad}sortingOrder   = {c.sortingOrder}");
        sb.AppendLine($"{pad}planeDistance  = {c.planeDistance}");
    }

    static void WriteUIText(StringBuilder sb, UnityEngine.UI.Text t, string pad)
    {
        sb.AppendLine($"{pad}text           = \"{t.text}\"");
        sb.AppendLine($"{pad}fontSize       = {t.fontSize}");
        sb.AppendLine($"{pad}fontStyle      = {t.fontStyle}");
        sb.AppendLine($"{pad}color          = {C(t.color)}");
        sb.AppendLine($"{pad}alignment      = {t.alignment}");
    }

    static void WriteUIImage(StringBuilder sb, UnityEngine.UI.Image i, string pad)
    {
        sb.AppendLine($"{pad}sprite         = {(i.sprite != null ? i.sprite.name : "null")}");
        sb.AppendLine($"{pad}color          = {C(i.color)}");
        sb.AppendLine($"{pad}imageType      = {i.type}");
        sb.AppendLine($"{pad}fillAmount     = {i.fillAmount}");
        sb.AppendLine($"{pad}raycastTarget  = {i.raycastTarget}");
    }

    static void WriteUIButton(StringBuilder sb, UnityEngine.UI.Button b, string pad)
    {
        sb.AppendLine($"{pad}interactable   = {b.interactable}");
        sb.AppendLine($"{pad}transition     = {b.transition}");
        int count = b.onClick.GetPersistentEventCount();
        sb.AppendLine($"{pad}onClick events = {count}");
        for (int i = 0; i < count; i++)
            sb.AppendLine($"{pad}  [{i}] {b.onClick.GetPersistentTarget(i)?.GetType().Name}.{b.onClick.GetPersistentMethodName(i)}");
    }

    static void WriteParticles(StringBuilder sb, ParticleSystem ps, string pad)
    {
        var main = ps.main;
        sb.AppendLine($"{pad}duration       = {main.duration}");
        sb.AppendLine($"{pad}looping        = {main.loop}");
        sb.AppendLine($"{pad}startLifetime  = {main.startLifetime.constant}");
        sb.AppendLine($"{pad}startSpeed     = {main.startSpeed.constant}");
        sb.AppendLine($"{pad}startSize      = {main.startSize.constant}");
        sb.AppendLine($"{pad}startColor     = {C(main.startColor.color)}");
        sb.AppendLine($"{pad}maxParticles   = {main.maxParticles}");
        sb.AppendLine($"{pad}playOnAwake    = {main.playOnAwake}");
        sb.AppendLine($"{pad}emission.rate  = {ps.emission.rateOverTime.constant}");
        sb.AppendLine($"{pad}simulationSpace= {main.simulationSpace}");
    }

    // ── Reflection fallback (catches MonoBehaviours & unknown components) ────
    static void WriteViaReflection(StringBuilder sb, Component comp, string pad, Type type, bool alreadyWrote)
    {
        const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.Public;
        bool header = alreadyWrote; // if we already wrote built-in props, skip re-header

        // Serialized fields (what you see in the Inspector)
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null) continue;
            if (SkipNames.Contains(field.Name)) continue;
            if (field.FieldType.IsSubclassOf(typeof(UnityEngine.Object)) && field.FieldType != typeof(GameObject)) continue;

            object val = null;
            try { val = field.GetValue(comp); } catch { continue; }
            if (val == null) continue;

            string str = FormatValue(val);
            if (str == null) continue;
            sb.AppendLine($"{pad}{field.Name,-20} = {str}");
        }

        // A few safe public properties (enabled, isActiveAndEnabled)
        foreach (var name in new[] { "enabled", "isActiveAndEnabled" })
        {
            var prop = type.GetProperty(name, FLAGS);
            if (prop == null) continue;
            try { sb.AppendLine($"{pad}{name,-20} = {prop.GetValue(comp)}"); } catch { }
        }
    }

    // ── Value formatter ───────────────────────────────────────────────────────
    static string FormatValue(object val)
    {
        if (val is Vector2 v2)  return V2(v2);
        if (val is Vector3 v3)  return V3(v3);
        if (val is Vector4 v4)  return $"({v4.x:F3},{v4.y:F3},{v4.z:F3},{v4.w:F3})";
        if (val is Quaternion q) return V3(q.eulerAngles) + " (euler)";
        if (val is Color c)     return C(c);
        if (val is bool b)      return b.ToString().ToLower();
        if (val is float f)     return f.ToString("F4");
        if (val is double d)    return d.ToString("F4");
        if (val is string s)    return $"\"{s}\"";
        if (val is Enum)        return val.ToString();
        if (val is int || val is uint || val is long || val is short || val is byte)
            return val.ToString();
        if (val is LayerMask lm) return LayerMask.LayerToName(lm);
        if (val is UnityEngine.Object uo) return uo != null ? uo.name : "null";

        // Skip complex / nested types silently
        return null;
    }

    // ── Utilities ─────────────────────────────────────────────────────────────
    static bool IsMonoBehaviour(Type t) =>
        t.IsSubclassOf(typeof(MonoBehaviour));

    static string Pad(int depth) => new string(' ', depth * 2);
    static string V3(Vector3 v)  => $"({v.x:F3}, {v.y:F3}, {v.z:F3})";
    static string V2(Vector2 v)  => $"({v.x:F3}, {v.y:F3})";
    static string C(Color c)     => $"rgba({c.r:F2},{c.g:F2},{c.b:F2},{c.a:F2})";
}
#endif
