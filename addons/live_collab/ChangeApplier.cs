using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public class ChangeApplier
{
    private EditorInterface _editorInterface;

    public ChangeApplier(EditorInterface editorInterface)
    {
        _editorInterface = editorInterface;
    }

    public void ApplyBatch(ChangeBatch batch)
    {
        foreach (var change in batch.Changes)
        {
            try
            {
                ApplyChange(change);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"LiveCollab: Error applying change: {ex.Message}");
            }
        }
    }

    private void ApplyChange(object change)
    {
        // Parse the change JSON
        string json = change.ToString();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("type", out var typeProp))
            return;

        var type = typeProp.GetString();

        switch (type)
        {
            case "set_property":
                ApplySetProperty(root);
                break;
            case "add_node":
                ApplyAddNode(root);
                break;
            case "remove_node":
                ApplyRemoveNode(root);
                break;
            default:
                GD.PrintErr($"LiveCollab: Unknown change type: {type}");
                break;
        }
    }

    private void ApplySetProperty(JsonElement change)
    {
        if (!change.TryGetProperty("node", out var nodeProp))
            return;
        if (!change.TryGetProperty("property", out var propertyProp))
            return;
        if (!change.TryGetProperty("value", out var valueProp))
            return;

        string nodePath = nodeProp.GetString();
        string property = propertyProp.GetString();

        // Get the node from the edited scene
        var editedScene = _editorInterface.GetEditedSceneRoot();
        if (editedScene == null)
            return;

        Node node = GetNodeByPath(editedScene, nodePath);
        if (node == null)
        {
            GD.PrintErr($"LiveCollab: Node not found: {nodePath}");
            return;
        }

        // Convert JSON value to Godot Variant
        var value = JsonValueToVariant(valueProp);

        // Set the property
        try
        {
            node.Set(property, value);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"LiveCollab: Failed to set property {property} on {nodePath}: {ex.Message}");
        }
    }

    private void ApplyAddNode(JsonElement change)
    {
        if (!change.TryGetProperty("parent", out var parentProp))
            return;
        if (!change.TryGetProperty("name", out var nameProp))
            return;
        if (!change.TryGetProperty("scene", out var sceneProp))
            return;

        string parentPath = parentProp.GetString();
        string name = nameProp.GetString();
        string scenePath = sceneProp.GetString();

        var editedScene = _editorInterface.GetEditedSceneRoot();
        if (editedScene == null)
            return;

        Node parent = GetNodeByPath(editedScene, parentPath);
        if (parent == null)
        {
            GD.PrintErr($"LiveCollab: Parent node not found: {parentPath}");
            return;
        }

        // Load and instantiate the scene
        try
        {
            var packedScene = GD.Load<PackedScene>(scenePath);
            if (packedScene == null)
            {
                GD.PrintErr($"LiveCollab: Failed to load scene: {scenePath}");
                return;
            }

            var instance = packedScene.Instantiate();
            instance.Name = name;
            parent.AddChild(instance);
            instance.Owner = editedScene;

            GD.Print($"LiveCollab: Added node {name} to {parentPath}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"LiveCollab: Failed to add node: {ex.Message}");
        }
    }

    private void ApplyRemoveNode(JsonElement change)
    {
        if (!change.TryGetProperty("node", out var nodeProp))
            return;

        string nodePath = nodeProp.GetString();

        var editedScene = _editorInterface.GetEditedSceneRoot();
        if (editedScene == null)
            return;

        Node node = GetNodeByPath(editedScene, nodePath);
        if (node == null)
        {
            GD.PrintErr($"LiveCollab: Node not found for removal: {nodePath}");
            return;
        }

        try
        {
            node.QueueFree();
            GD.Print($"LiveCollab: Removed node {nodePath}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"LiveCollab: Failed to remove node: {ex.Message}");
        }
    }

    private Node GetNodeByPath(Node root, string path)
    {
        try
        {
            return root.GetNode(path);
        }
        catch
        {
            return null;
        }
    }

    private Variant JsonValueToVariant(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
                
            case JsonValueKind.Number:
                if (element.TryGetInt32(out int intValue))
                    return intValue;
                if (element.TryGetInt64(out long longValue))
                    return longValue;
                return element.GetDouble();
                
            case JsonValueKind.True:
                return true;
                
            case JsonValueKind.False:
                return false;
                
            case JsonValueKind.Array:
                return ParseJsonArray(element);
                
            case JsonValueKind.Object:
                return ParseJsonObject(element);
                
            default:
                return new Variant();
        }
    }

    private Variant ParseJsonArray(JsonElement element)
    {
        var list = new Godot.Collections.Array();
        
        foreach (var item in element.EnumerateArray())
        {
            list.Add(JsonValueToVariant(item));
        }

        // Try to convert to specific types
        if (list.Count == 2)
        {
            // Could be Vector2
            if (list[0].VariantType == Variant.Type.Float || list[0].VariantType == Variant.Type.Int)
            {
                return new Vector2((float)list[0], (float)list[1]);
            }
        }
        else if (list.Count == 3)
        {
            // Could be Vector3
            if (list[0].VariantType == Variant.Type.Float || list[0].VariantType == Variant.Type.Int)
            {
                return new Vector3((float)list[0], (float)list[1], (float)list[2]);
            }
        }
        else if (list.Count == 4)
        {
            // Could be Quaternion or Color
            if (list[0].VariantType == Variant.Type.Float || list[0].VariantType == Variant.Type.Int)
            {
                return new Quaternion((float)list[0], (float)list[1], (float)list[2], (float)list[3]);
            }
        }

        return list;
    }

    private Variant ParseJsonObject(JsonElement element)
    {
        var dict = new Godot.Collections.Dictionary();
        
        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = JsonValueToVariant(property.Value);
        }

        return dict;
    }
}
