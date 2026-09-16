using System;
using UnityEngine.Events;

[Serializable]
public sealed class PerceptionTelemetryMessage
{
    public string type;
    public int schema_version;
    public string source;
    public string session_id;
    public long seq;
    public long sent_at_ms;
    public string state;
    public bool video_ok;
    public bool detected;
    public bool fire_detected;
    public bool candidate_detected;
    public string target;
    public float center_x;
    public float center_y;
    public float fire_center_x;
    public float fire_center_y;
    public float radius;
    public float score;
    public float fire_confidence;
    public float circularity;
    public float aspect_ratio;
    public int frame_width;
    public int frame_height;
    public float processing_ms;
    public bool dynamic_obstacle;
    public string obstacle_direction;
    public bool free_left;
    public bool free_front;
    public bool free_right;
    public bool autonomy_valid;
    public float auto_v;
    public float auto_steer;
    public bool auto_extinguish;
    public string auto_state;

    public bool IsSupported()
    {
        return string.Equals(type, "perception", StringComparison.Ordinal) &&
               (schema_version == 1 || schema_version == 2) &&
               !string.IsNullOrEmpty(session_id) &&
               seq > 0;
    }

    public bool FireDetected => schema_version >= 2 ? fire_detected : detected;
    public float FireCenterX => schema_version >= 2 ? fire_center_x : center_x;
    public float FireCenterY => schema_version >= 2 ? fire_center_y : center_y;
    public float FireConfidence => schema_version >= 2 ? fire_confidence : score;
}

[Serializable]
public sealed class PerceptionTelemetryEvent : UnityEvent<PerceptionTelemetryMessage>
{
}
