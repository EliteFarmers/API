using System.Text.Json;
using EliteAPI.Features.ToolSettings.Services;
using Ganss.Xss;

namespace EliteAPI.Tests;

public class ToolSettingsJsonGuardTests
{
	[Fact]
	public void Validate_Allows_Safe_Structure() {
		using var doc = JsonDocument.Parse("""
		{
		  "Root": {
		    "Child1": [1, 2, 3],
		    "Child2": true,
		    "Child3": "value"
		  }
		}
		""");

		var valid = ToolSettingsJsonGuard.TryValidate(doc.RootElement, out var error);

		valid.ShouldBeTrue();
		error.ShouldBeNull();
	}

	[Fact]
	public void Validate_Rejects_TooDeep_Payload() {
		using var doc = JsonDocument.Parse("""
		{
		  "A": {
		    "B": {
		      "C": {
		        "D": {
		          "E": {
		            "F": {
		              "G": {
		                "H": {
		                  "I": 1
		                }
		              }
		            }
		          }
		        }
		      }
		    }
		  }
		}
		""");

		var valid = ToolSettingsJsonGuard.TryValidate(doc.RootElement, out var error);

		valid.ShouldBeFalse();
		error.ShouldNotBeNull();
		error.ShouldContain("Maximum JSON depth");
	}

	[Fact]
	public void Validate_Rejects_Array_Over_Max_Length() {
		var items = string.Join(",", Enumerable.Range(0, 101));
		using var doc = JsonDocument.Parse($"{{\"A\":[{items}]}}");

		var valid = ToolSettingsJsonGuard.TryValidate(doc.RootElement, out var error);

		valid.ShouldBeFalse();
		error.ShouldNotBeNull();
		error.ShouldContain("Maximum array length");
	}

	[Fact]
	public void Validate_Rejects_Non_Alphanumeric_Key() {
		using var doc = JsonDocument.Parse("""
		{
		  "bad_key": 1
		}
		""");

		var valid = ToolSettingsJsonGuard.TryValidate(doc.RootElement, out var error);

		valid.ShouldBeFalse();
		error.ShouldNotBeNull();
		error.ShouldContain("Invalid key");
	}

	[Fact]
	public void SanitizeStrings_Sanitizes_Strings_And_Preserves_Number_Bool() {
		var sanitizer = new HtmlSanitizer();
		using var doc = JsonDocument.Parse("""
		{
		  "A": "<script>alert(1)</script>hello",
		  "B": 123,
		  "C": false,
		  "D": ["<script>x</script>ok"]
		}
		""");

		using var sanitized = ToolSettingsJsonGuard.SanitizeStrings(doc.RootElement, sanitizer);
		var root = sanitized.RootElement;

		root.GetProperty("A").GetString()!.Contains("<script", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
		root.GetProperty("B").GetInt32().ShouldBe(123);
		root.GetProperty("C").GetBoolean().ShouldBeFalse();
		root.GetProperty("D")[0].GetString()!.Contains("<script", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
	}
}
