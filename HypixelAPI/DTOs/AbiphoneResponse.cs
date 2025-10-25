using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace EliteFarmers.HypixelAPI.DTOs;

public class MemberAbiphoneResponse
{
    [JsonPropertyName("contact_data")] public Dictionary<string, AbiphoneContactResponse> ContactData { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Arbitrary per-mini-game stats (snake, tic tac toe, etc)
    /// </summary>
    [JsonPropertyName("games")] public JsonObject? Games { get; set; }

    [JsonPropertyName("active_contacts")] public List<string> ActiveContacts { get; set; } = [];

    [JsonPropertyName("operator_chip")] public AbiphoneOperatorChip? OperatorChip { get; set; }

    [JsonPropertyName("trio_contact_addons")] public int? TrioContactAddons { get; set; }

    [JsonPropertyName("selected_sort")] public string? SelectedSort { get; set; }

    [JsonPropertyName("selected_ringtone")] public string? SelectedRingtone { get; set; }

    [JsonPropertyName("has_used_sirius_personal_phone_number_item")] public bool? HasUsedSiriusPersonalPhoneNumberItem { get; set; }

    [JsonPropertyName("last_dye_called_year")] public int? LastDyeCalledYear { get; set; }
}

public class AbiphoneContactResponse
{
    [JsonPropertyName("talked_to")] public bool? TalkedTo { get; set; }

    [JsonPropertyName("completed_quest")] public bool? CompletedQuest { get; set; }

    [JsonPropertyName("last_call")] public long? LastCall { get; set; }

    [JsonPropertyName("dnd_enabled")] public bool? DoNotDisturbEnabled { get; set; }

    [JsonPropertyName("incoming_calls_count")] public int? IncomingCallsCount { get; set; }

    [JsonPropertyName("last_call_incoming")] public long? LastIncomingCall { get; set; }

    /// <summary>
    /// Contact-specific metadata (quest flags, rewards, etc)
    /// </summary>
    [JsonPropertyName("specific")] public JsonObject? Specific { get; set; }
}

public class AbiphoneOperatorChip
{
    [JsonPropertyName("repaired_index")] public int? RepairedIndex { get; set; }
}
