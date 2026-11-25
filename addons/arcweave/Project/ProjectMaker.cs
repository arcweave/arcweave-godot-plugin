using System.Linq;
using Arcweave.Interpreter.INodes;
using Godot;
using Godot.Collections;

namespace Arcweave.Project
{
	internal class ProjectMaker
	{
		private Element _startingElement;
		private readonly Dictionary _projectData;
		private readonly Dictionary _boardsDict;
		private readonly Dictionary _attributesDict;
		private readonly Dictionary _componentsDict;
		private readonly Dictionary _conditionsDict;
		private readonly Dictionary _connectionsDict;
		private readonly Dictionary _elementsDict;
		private readonly Dictionary _jumpersDict;
		private readonly Dictionary _branchesDict;
		private readonly Dictionary _variablesDict;
		private readonly Dictionary _notesDict;
		private readonly Dictionary _assetsDict;
		
		private readonly Dictionary<string, Board> _boards;
		private readonly Dictionary<string, Attribute> _attributes;
		private readonly Dictionary<string, Component> _components;
		private readonly Dictionary<string, Condition> _conditions;
		private readonly Dictionary<string, Connection> _connections;
		private readonly Dictionary<string, Element> _elements;
		private readonly Dictionary<string, Jumper> _jumpers;
		private readonly Dictionary<string, Branch> _branches;
		private readonly Array<Variable> _variables;
		private readonly Dictionary<string, Variable> _variablesById;
		private readonly Dictionary<string, Note> _notes;
		private readonly Dictionary<string, Asset> _assets;
		private readonly Dictionary _mediaPaths;

		/// <param name="projectData">The project data</param>
		/// <param name="mediaPaths">The res:// paths of the downloaded media by Arcweave asset ID</param>
		public ProjectMaker(Dictionary projectData, Dictionary mediaPaths = null)
		{
			_projectData = projectData;
			_mediaPaths = mediaPaths ?? new Dictionary();
			_boardsDict = GetDictionary(_projectData, "boards");
			_attributesDict = GetDictionary(_projectData, "attributes");
			_componentsDict = GetDictionary(_projectData, "components");
			_conditionsDict = GetDictionary(_projectData, "conditions");
			_connectionsDict = GetDictionary(_projectData, "connections");
			_elementsDict = GetDictionary(_projectData, "elements");
			_jumpersDict = GetDictionary(_projectData, "jumpers");
			_branchesDict = GetDictionary(_projectData, "branches");
			_variablesDict = GetDictionary(_projectData, "variables");
			_notesDict = GetDictionary(_projectData, "notes");
			_assetsDict = GetDictionary(_projectData, "assets");
			
			_boards = new Dictionary<string, Board>();
			_attributes = new Dictionary<string, Attribute>();
			_components = new Dictionary<string, Component>();
			_conditions = new Dictionary<string, Condition>();
			_connections = new Dictionary<string, Connection>();
			_elements = new Dictionary<string, Element>();
			_jumpers = new Dictionary<string, Jumper>();
			_branches = new Dictionary<string, Branch>();
			_variables = new Array<Variable>();
			_variablesById = new Dictionary<string, Variable>();
			_notes = new Dictionary<string, Note>();
			_assets = new Dictionary<string, Asset>();
		}

		public Project MakeProject()
		{
			Project project = new Project(GetString(_projectData, "name"));

			Variant assetRoot = _assetsDict.Values.FirstOrDefault(asset =>
			{
				Dictionary assetDict = asset.AsGodotDictionary();
				return assetDict.ContainsKey("root") && assetDict["root"].AsBool();
			});
			if (assetRoot.VariantType == Variant.Type.Dictionary)
			{
				MakeAssets(GetStringList(assetRoot.AsGodotDictionary(), "children"), "");
			}
			
			foreach (string key in _attributesDict.Keys)
			{
				_attributes[key] = new Attribute(project);
			}

			foreach (string key in _componentsDict.Keys)
			{
				Dictionary comp = _componentsDict[key].AsGodotDictionary();
				if (IsFolder(comp))
				{
					continue;
				}
				Asset coverAsset = null;
				var cover = GetDictionary(GetDictionary(comp, "assets"), "cover");
				if (GetString(cover, "type") is "icon")
				{
					coverAsset = new Asset(GetString(cover, "file"), GetString(cover, "file"), "",
						Asset.AssetType.Icon);
				}
				else if (GetString(cover, "id").Length > 0)
				{
					coverAsset = _assets[GetString(cover, "id")];
				}
				string customId = GetStringOrNull(comp, "customId");
				_components[key] = new Component(key, customId, GetString(comp, "name"), coverAsset);
				foreach (string attrId in GetStringList(comp, "attributes"))
				{
					_components[key].AddAttribute(_attributes[attrId]);
				}
			}

			foreach (string key in _attributesDict.Keys)
			{
				Dictionary attr = _attributesDict[key].AsGodotDictionary();
				IAttribute.DataType attrType = IAttribute.DataType.Undefined;
				Dictionary attrValue = GetDictionary(attr, "value");
				string valueType = GetString(attrValue, "type");
				object data = null;
				if (valueType == "string")
				{
					if (attrValue.ContainsKey("plain") && attrValue["plain"].AsBool())
					{
						attrType = IAttribute.DataType.StringPlainText;
						data = !attrValue.ContainsKey("data") || attrValue["data"].VariantType == Variant.Type.Nil
							? string.Empty
							: attrValue["data"].AsString();
					}
					else
					{
						attrType = IAttribute.DataType.StringRichText;
						data = !attrValue.ContainsKey("data") || attrValue["data"].VariantType == Variant.Type.Nil
							? string.Empty
							: attrValue["data"].AsString();
					}
				}
				else if (valueType == "component-list")
				{
					attrType = IAttribute.DataType.ComponentList;
					Array<Component> attrComps = new();
					foreach (string compId in GetStringList(attrValue, "data"))
					{
						attrComps.Add(_components[compId]);
					}
					data = attrComps;
				}
				else if (valueType == "asset-list")
				{
					attrType = IAttribute.DataType.AssetList;
					Array<Asset> attrAssets = new();
					foreach (string assetId in GetStringList(attrValue, "data"))
					{
						attrAssets.Add(_assets[assetId]);
					}
					data = attrAssets;
				}
				else if (valueType == "boolean" && attrValue.ContainsKey("data") &&
				         attrValue["data"].VariantType != Variant.Type.Nil)
				{
					attrType = IAttribute.DataType.Boolean;
					data = attrValue["data"].AsBool();
				}
				else if (valueType == "integer" && attrValue.ContainsKey("data") &&
				         attrValue["data"].VariantType != Variant.Type.Nil)
				{
					attrType = IAttribute.DataType.Integer;
					data = attrValue["data"].AsInt32();
				}
				else if (valueType == "float" && attrValue.ContainsKey("data") &&
				         attrValue["data"].VariantType != Variant.Type.Nil)
				{
					attrType = IAttribute.DataType.Float;
					data = attrValue["data"].AsDouble();
				}

				var containerType = GetString(attr, "cType") switch
				{
					"elements" => IAttribute.ContainerType.Element,
					"components" => IAttribute.ContainerType.Component,
					"boards" => IAttribute.ContainerType.Board,
					_ => IAttribute.ContainerType.Undefined
				};
				string customId = GetStringOrNull(attr, "customId");

				_attributes[key].Set(key, customId, GetString(attr, "name"), attrType, data, containerType, GetString(attr, "cId"));
			}

			foreach (string key in _connectionsDict.Keys)
			{
				_connections[key] = new Connection(key);
			}

			foreach (string key in _elementsDict.Keys)
			{
				Dictionary el = _elementsDict[key].AsGodotDictionary();
				Array<Connection> elConnections = new();
				foreach (string output in GetStringList(el, "outputs"))
				{
					elConnections.Add(_connections[output]);
				}

				Array<Attribute> elAttributes = new();
				foreach (string attrId in GetStringList(el, "attributes"))
				{
					elAttributes.Add(_attributes[attrId]);
				}

				Array<Component> elComponents = new();
				foreach (string componentId in GetStringList(el, "components"))
				{
					elComponents.Add(_components[componentId]);
				}

				Asset coverAsset = null;
				string coverId = GetString(GetDictionary(GetDictionary(el, "assets"), "cover"), "id");
				if (coverId.Length > 0)
				{
					coverAsset = _assets[coverId];
				}
				_elements[key] = new Element(key, GetString(el, "title"), GetString(el, "content"), project, elConnections, elComponents, elAttributes, coverAsset);
			}

			// A project without a starting element has none
			string startingElementId = GetString(_projectData, "startingElement");
			_startingElement = startingElementId.Length > 0 && _elements.TryGetValue(startingElementId, out var startingElement)
				? startingElement
				: null;

			foreach (string key in _jumpersDict.Keys)
			{
				Dictionary jumper = _jumpersDict[key].AsGodotDictionary();
				Element target = null;
				if (jumper.ContainsKey("elementId") && jumper["elementId"].AsString().Length > 0)
				{
					target = _elements[jumper["elementId"].AsString()];
				}
				_jumpers[key] = new Jumper(key, project, target);
			}

			foreach (string key in _conditionsDict.Keys)
			{
				Dictionary cond = _conditionsDict[key].AsGodotDictionary();
				Connection condOutput = null;
				string script = null;
				string outputId = GetString(cond, "output");
				if (outputId.Length > 0)
				{
					condOutput = _connections[outputId];
				}
				if (cond.ContainsKey("script") && cond["script"].VariantType != Variant.Type.Nil)
				{
					script = cond["script"].AsString();
				}
				_conditions[key] = new Condition(key, project, script, condOutput);
			}

			foreach (string key in _branchesDict.Keys)
			{
				Dictionary branch = _branchesDict[key].AsGodotDictionary();
				Dictionary branchConditions = GetDictionary(branch, "conditions");
				Array<Condition> branchConditionList = new();
				string ifCondition = GetString(branchConditions, "ifCondition");
				if (ifCondition.Length > 0)
				{
					branchConditionList.Add(_conditions[ifCondition]);
				}
				foreach (string condKey in GetStringList(branchConditions, "elseIfConditions"))
				{
					branchConditionList.Add(_conditions[condKey]);
				}
				string elseCondition = GetString(branchConditions, "elseCondition");
				if (elseCondition.Length > 0)
				{
					branchConditionList.Add(_conditions[elseCondition]);
				}

				_branches[key] = new Branch(key, project, branchConditionList);
			}

			foreach (string key in _connectionsDict.Keys)
			{
				Dictionary con = _connectionsDict[key].AsGodotDictionary();
				INode source, target;
				switch (GetString(con, "sourceType"))
				{
					case "elements":
						source = _elements[con["sourceid"].AsString()];
						break;
					case "branches":
						source = _branches[con["sourceid"].AsString()];
						break;
					case "jumpers":
						source = _jumpers[con["sourceid"].AsString()];
						break;
                    case "conditions":
                        source = _conditions[con["sourceid"].AsString()];
                        break;
                    default:
						source = null;
						break;
				}
				switch (GetString(con, "targetType"))
				{
					case "elements":
						target = _elements[con["targetid"].AsString()];
						break;
					case "branches":
						target = _branches[con["targetid"].AsString()];
						break;
					case "jumpers":
						target = _jumpers[con["targetid"].AsString()];
						break;
                    case "conditions":
                        target = _conditions[con["targetid"].AsString()];
                        break;
                    default:
						target = null;
						break;
				}

				_connections[key].Set(GetString(con, "label"), source, target);
			}

			foreach (string key in _boardsDict.Keys)
			{
				Dictionary board = _boardsDict[key].AsGodotDictionary();
				if (IsFolder(board))
				{
					continue;
				}
				Array<Element> boardElements = new();
				foreach (string elementKey in GetStringList(board, "elements"))
				{
					boardElements.Add(_elements[elementKey]);
				}

				Array<Jumper> boardJumpers = new();
				foreach (string jumperKey in GetStringList(board, "jumpers"))
				{
					boardJumpers.Add(_jumpers[jumperKey]);
				}

				Array<Branch> boardBranches = new();
				foreach (string branchKey in GetStringList(board, "branches"))
				{
					boardBranches.Add(_branches[branchKey]);
				}

				Array<Connection> boardConnections = new();
				foreach (string connectionKey in GetStringList(board, "connections"))
				{
					boardConnections.Add(_connections[connectionKey]);
				}

				Array<Note> boardNotes = new();
				foreach (string noteKey in GetStringList(board, "notes"))
				{
					Dictionary note = _notesDict[noteKey].AsGodotDictionary();
					_notes[noteKey] = new Note(Interpreter.Utils.CleanString(GetString(note, "content")), GetString(note, "theme"));
					boardNotes.Add(_notes[noteKey]);
				}

				string customId = GetStringOrNull(board, "customId");

				_boards[key] = new Board(key, GetString(board, "name"), customId, boardElements, boardConnections, boardJumpers, boardBranches, boardNotes);
				foreach (string attributeId in GetStringList(board, "attributes"))
				{
					if (_attributes.TryGetValue(attributeId, out var attribute))
					{
						_boards[key].AddAttribute(attribute);
					}
				}
			}

			foreach (string key in _variablesDict.Keys)
			{
				Dictionary variable = _variablesDict[key].AsGodotDictionary();
				if (IsFolder(variable)) continue;
				if (!TryGetVariableValue(variable, out var value))
				{
					continue;
				}

				string containerType = variable.ContainsKey("cType") ? variable["cType"].AsString() : "global";
				Variable runtimeVariable;
				if (containerType == "boards")
				{
					string boardId = variable.ContainsKey("cId") ? variable["cId"].AsString() : null;
					if (string.IsNullOrEmpty(boardId) || !_boards.TryGetValue(boardId, out var board))
					{
						GD.PushWarning($"[Arcweave] Variable '{variable["name"].AsString()}' references a missing board '{boardId}'.");
						continue;
					}
					runtimeVariable = new Variable(key, variable["name"].AsString(), value, board);
					board.AddVariable(runtimeVariable);
				}
				else
				{
					runtimeVariable = new Variable(key, variable["name"].AsString(), value);
					_variables.Add(runtimeVariable);
				}

				_variablesById[key] = runtimeVariable;
			}

			MakeAttributeVariables();

			project.Set(_startingElement, _boards, _components, _variables, _elements, _assets);
			// Keeps the ID from the project data also when no element has it, so errors can name it
			project.StartingElementId = GetString(_projectData, "startingElement");
			return project;
		}

		// Arcweave exports omit empty lists and unset values, and may write them as null, so a missing or null value
		// is read as empty.

		/// <summary>Returns the list of strings under key, or an empty list if it is missing or null.</summary>
		private static string[] GetStringList(Dictionary dict, string key)
		{
			return dict.TryGetValue(key, out var value) && value.VariantType != Variant.Type.Nil
				? value.AsStringArray()
				: System.Array.Empty<string>();
		}

		/// <summary>Returns the dictionary under key, or an empty dictionary if it is missing or null.</summary>
		private static Dictionary GetDictionary(Dictionary dict, string key)
		{
			return dict.TryGetValue(key, out var value) && value.VariantType == Variant.Type.Dictionary
				? value.AsGodotDictionary()
				: new Dictionary();
		}

		/// <summary>
		/// Returns true for folders, including the root folder. An empty folder's children may be omitted or null, so the
		/// key is checked instead of its value.
		/// </summary>
		private static bool IsFolder(Dictionary dict)
		{
			return dict.ContainsKey("children") || (dict.TryGetValue("root", out var root) && root.VariantType == Variant.Type.Bool && root.AsBool());
		}

		/// <summary>Returns the string under key, or null if it is missing or null.</summary>
		private static string GetStringOrNull(Dictionary dict, string key)
		{
			return dict.TryGetValue(key, out var value) && value.VariantType != Variant.Type.Nil ? value.AsString() : null;
		}

		/// <summary>Returns the string under key, or an empty string if it is missing or null.</summary>
		private static string GetString(Dictionary dict, string key)
		{
			return dict.TryGetValue(key, out var value) && value.VariantType != Variant.Type.Nil
				? value.AsString()
				: string.Empty;
		}

		private bool TryGetVariableValue(Dictionary variable, out Variant value)
		{
			value = default;
			if (!variable.ContainsKey("type") || !variable.ContainsKey("value") ||
				variable["value"].VariantType == Variant.Type.Nil)
			{
				return false;
			}

			switch (variable["type"].AsString())
			{
				case "integer":
					value = variable["value"].AsInt32();
					return true;
				case "float":
					value = variable["value"].AsDouble();
					return true;
				case "string":
					value = variable["value"].AsString();
					return true;
				case "boolean":
					value = variable["value"].AsBool();
					return true;
				default:
					GD.PushWarning("[Arcweave] Variable type \"" + variable["type"].AsString() + "\" not found");
					return false;
			}
		}

		private void MakeAttributeVariables()
		{
			foreach (var board in _boards.Values)
			{
				MakeAttributeVariables(board, board.Id, IAttribute.ContainerType.Board, board.Attributes);
			}

			foreach (var component in _components.Values)
			{
				MakeAttributeVariables(component, component.Id, IAttribute.ContainerType.Component, component.Attributes);
			}
		}

		private void MakeAttributeVariables(IHasVariables container, string containerId,
			IAttribute.ContainerType containerType, Array<Attribute> attributes)
		{
			if (string.IsNullOrEmpty(container.CustomId)) return;

			foreach (var attribute in attributes)
			{
				if (!IsVariableAttribute(attribute, containerId, containerType) ||
					_variablesById.ContainsKey(attribute.Id))
				{
					continue;
				}

				var variable = new Variable(attribute.Id, attribute.CustomId, attribute.data, container);
				_variablesById[attribute.Id] = variable;
				container.AddVariable(variable);
			}
		}

		private static bool IsVariableAttribute(Attribute attribute, string containerId,
			IAttribute.ContainerType containerType)
		{
			if (attribute == null || string.IsNullOrEmpty(attribute.CustomId)) return false;
			if (attribute.containerType != containerType || attribute.containerId != containerId) return false;

			return attribute.Type == IAttribute.DataType.Boolean ||
			       attribute.Type == IAttribute.DataType.Integer ||
			       attribute.Type == IAttribute.DataType.Float ||
			       attribute.Type == IAttribute.DataType.StringPlainText;
		}

		public void MakeAssets(System.Collections.Generic.IEnumerable<string> assetIds, string path)
		{
			foreach (var assetId in assetIds)
			{
				var asset = _assetsDict[assetId].AsGodotDictionary();
				// An empty folder may omit its children, but assets always have a type
				if (IsFolder(asset) || !asset.ContainsKey("type"))
				{
					string assetName = asset.ContainsKey("root") && asset["root"].AsBool()
						? ""
						: GetString(asset, "name");
					MakeAssets(GetStringList(asset, "children"), path + "/" + assetName);
				}
				else
				{
					string assetType = GetString(asset, "type");
					Asset.AssetType type = assetType switch
					{
						"image" or "template-image" => Asset.AssetType.Image,
						"audio" or "template-audio" => Asset.AssetType.Audio,
						_ => Asset.AssetType.Undefined
					};
					string assetName = GetString(asset, "name");
					_assets[assetId] = new Asset(assetId, assetName, path + '/' + assetName, type);
					if (_mediaPaths.TryGetValue(assetId, out var localPath))
					{
						_assets[assetId].LocalPath = localPath.AsString();
					}
				}
			}
		}
	}
}
