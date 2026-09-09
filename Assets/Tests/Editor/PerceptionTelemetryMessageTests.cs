using NUnit.Framework;
using UnityEngine;

public sealed class PerceptionTelemetryMessageTests
{
    [Test]
    public void ParsesSupportedPacket()
    {
        const string json =
            "{\"type\":\"perception\",\"schema_version\":1," +
            "\"source\":\"unit_test\",\"session_id\":\"session-1\"," +
            "\"seq\":3,\"sent_at_ms\":1234,\"state\":\"detected\"," +
            "\"video_ok\":true,\"detected\":true,\"candidate_detected\":true," +
            "\"target\":\"red_ball\",\"center_x\":216.0,\"center_y\":231.0," +
            "\"radius\":6.2,\"score\":0.4,\"circularity\":0.8," +
            "\"aspect_ratio\":1.1,\"frame_width\":640,\"frame_height\":480," +
            "\"processing_ms\":5.5}";

        PerceptionTelemetryMessage message =
            JsonUtility.FromJson<PerceptionTelemetryMessage>(json);

        Assert.That(message.IsSupported(), Is.True);
        Assert.That(message.detected, Is.True);
        Assert.That(message.center_x, Is.EqualTo(216.0f));
        Assert.That(message.seq, Is.EqualTo(3));
    }

    [Test]
    public void RejectsUnexpectedSchema()
    {
        const string json =
            "{\"type\":\"perception\",\"schema_version\":3," +
            "\"session_id\":\"session-1\",\"seq\":1}";
        PerceptionTelemetryMessage message =
            JsonUtility.FromJson<PerceptionTelemetryMessage>(json);
        Assert.That(message.IsSupported(), Is.False);
    }

    [Test]
    public void ParsesCourseSchemaV2FireAndObstacleFields()
    {
        const string json =
            "{\"type\":\"perception\",\"schema_version\":2," +
            "\"session_id\":\"session-2\",\"seq\":8," +
            "\"video_ok\":true,\"fire_detected\":true," +
            "\"fire_center_x\":418.2,\"fire_center_y\":236.0," +
            "\"fire_confidence\":0.91,\"dynamic_obstacle\":true," +
            "\"obstacle_direction\":\"front\",\"free_front\":false}";

        PerceptionTelemetryMessage message = JsonUtility.FromJson<PerceptionTelemetryMessage>(json);

        Assert.That(message.IsSupported(), Is.True);
        Assert.That(message.FireDetected, Is.True);
        Assert.That(message.FireCenterX, Is.EqualTo(418.2f));
        Assert.That(message.FireConfidence, Is.EqualTo(0.91f));
        Assert.That(message.dynamic_obstacle, Is.True);
        Assert.That(message.free_front, Is.False);
    }
}
