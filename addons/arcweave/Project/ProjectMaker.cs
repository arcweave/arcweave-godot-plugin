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
		private readonly Dictionary<string, Note> _notes;
		private readonly Dictionary<string, Asset> _assets;

		public ProjectMaker(Dictionary projectData)
		{
			_projectData = projectData;
			_boardsDict = _projectData["boards"].AsGodotDictionary();
			_attributesDict = _projectData["attributes"].AsGodotDictionary();
			_componentsDict = _projectData["components"].AsGodotDictionary();
			_conditionsDict = _projectData["conditions"].AsGodotDictionary();
			_connectionsDict = _projectData["connections"].AsGodotDictionary();
			_elementsDict = _projectData["elements"].AsGodotDictionary();
			_jumpersDict = _projectData["jumpers"].AsGodotDictionary();
			_branchesDict = _projectData["branches"].AsGodotDictionary();
			_variablesDict = _projectData["variables"].AsGodotDictionary();
			_notesDict = _projectData["notes"].AsGodotDictionary();
			_assetsDict = _projectData["assets"].AsGodotDictionary();
			
			_boards = new Dictionary<string, Board>();
			_attributes = new Dictionary<string, Attribute>();
			_components = new Dictionary<string, Component>();
			_conditions = new Dictionary<string, Condition>();
			_connections = new Dictionary<string, Connection>();
			_elements = new Dictionary<string, Element>();
			_jumpers = new Dictionary<string, Jumper>();
			_branches = new Dictionary<string, Branch>();
			_variables = new Array<Variable>();
			_notes = new Dictionary<string, Note>();
			_assets = new Dictionary<string, Asset>();
		}

		public Project MakeProject()
		{
			Project project = new Project(_projectData["name"].AsString());

			Dictionary assetRoot = _assetsDict.Values.First(asset =>
			{
				Dictionary assetDict = asset.AsGodotDictionary();
				return assetDict.ContainsKey("root") && assetDict["root"].AsBool();
			}).AsGodotDictionary();

			MakeAssets(assetRoot["children"].AsGodotArray<string>(), "");
			
			foreach (string key in _attributesDict.Keys)
			{
				_attributes[key] = new Attribute(project);
			}

			foreach (string key in _componentsDict.Keys)
			{
				Dictionary comp = _componentsDict[key].AsGodotDictionary();
				if (comp.ContainsKey("children"))
				{
					continue;
				}
				Asset coverAsset = null;
				if (comp.ContainsKey("assets"))
				{
					var compAssets = comp["assets"].AsGodotDictionary();
					if (compAssets.ContainsKey("cover"))
					{
						var cover = compAssets["cover"].AsGodotDictionary();
						if (cover.ContainsKey("type") && cover["type"].AsString() is "icon")
						{
							coverAsset = new Asset(cover["file"].AsString(), cover["file"].AsString(), "",
								Asset.AssetType.Icon);
						}
						else
						{
							coverAsset = _assets[cover["id"].AsString()];
						}
					}
				}
				_components[key] = new Component(key, comp["name"].AsString(), coverAsset);
				foreach (string attrId in comp["attributes"].AsStringArray())
				{
					_components[key].AddAttribute(_attributes[attrId]);
				}
			}

			foreach (string key in _attributesDict.Keys)
			{
				Dictionary attr = _attributesDict[key].AsGodotDictionary();
				IAttribute.DataType attrType;
				Dictionary attrValue = attr["value"].AsGodotDictionary();
				object data;
				if (attrValue["type"].AsString() == "string")
				{
					if (attrValue.ContainsKey("plain") && attrValue["plain"].AsBool())
					{
						attrType = IAttribute.DataType.StringPlainText;
					}
					else
					{
						attrType = IAttribute.DataType.StringRichText;
					}
					var dataString = attrValue["data"].AsString();
					data = dataString;
				}
				else
				{
					attrType = IAttribute.DataType.ComponentList;
					Array<Component> attrComps = new();
					foreach (string compId in attrValue["data"].AsStringArray())
					{
						attrComps.Add(_components[compId]);
					}
					data = attrComps;
				}

				var containerType = attr["cType"].AsString() == "elements" ? IAttribute.ContainerType.Element : IAttribute.ContainerType.Component;

				_attributes[key].Set(key, attr["name"].AsString(), attrType, data, containerType, attr["cId"].AsString());
			}

			foreach (string key in _connectionsDict.Keys)
			{
				_connections[key] = new Connection(key);
			}

			foreach (string key in _elementsDict.Keys)
			{
				Dictionary el = _elementsDict[key].AsGodotDictionary();
				Array<Connection> elConnections = new();
				foreach (string output in el["outputs"].AsStringArray())
				{
					elConnections.Add(_connections[output]);
				}

				Array<Attribute> elAttributes = new();
				if (el.ContainsKey("attributes"))
				{
					foreach (string attrId in el["attributes"].AsStringArray())
					{
						elAttributes.Add(_attributes[attrId]);
					}
				}

				Array<Component> elComponents = new();
				if (el.ContainsKey("components"))
				{
					foreach (string componentId in el["components"].AsStringArray())
					{
						elComponents.Add(_components[componentId]);
					}
				}

				Asset coverAsset = null;
				if (el.ContainsKey("assets"))
				{
					var elAssets = el["assets"].AsGodotDictionary();
					if (elAssets.ContainsKey("cover"))
					{
						var cover = elAssets["cover"].AsGodotDictionary();
						if (cover.ContainsKey("id"))
						{
							coverAsset = _assets[cover["id"].AsString()];
						}
					}
				}
				_elements[key] = new Element(key, el["title"].AsString(), el["content"].AsString(), project, elConnections, elComponents, elAttributes, coverAsset);
			}

			_startingElement = _elements[_projectData["startingElement"].AsString()];

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
				if (cond.ContainsKey("output") && cond["output"].VariantType != Variant.Type.Nil)
				{
					condOutput = _connections[cond["output"].AsString()];
				}
				if (cond["script"].Obj != null)
				{
					script = cond["script"].AsString();
				}
				_conditions[key] = new Condition(key, project, script, condOutput);
			}

			foreach (string key in _branchesDict.Keys)
			{
				Dictionary branch = _branchesDict[key].AsGodotDictionary();
				Dictionary branchConditions = branch["conditions"].AsGodotDictionary();
				Array<Condition> branchConditionList = new();
				if (branchConditions.ContainsKey("ifCondition"))
				{
					branchConditionList.Add(_conditions[branchConditions["ifCondition"].AsString()]);
				}
				if (branchConditions.ContainsKey("elseIfConditions"))
				{
					foreach (string condKey in branchConditions["elseIfConditions"].AsStringArray())
					{
						branchConditionList.Add(_conditions[condKey]);
					}
				}
				if (branchConditions.ContainsKey("elseCondition"))
				{
					branchConditionList.Add(_conditions[branchConditions["elseCondition"].AsString()]);
				}

				_branches[key] = new Branch(key, project, branchConditionList);
			}

			foreach (string key in _connectionsDict.Keys)
			{
				Dictionary con = _connectionsDict[key].AsGodotDictionary();
				INode source, target;
				switch (con["sourceType"].AsString())
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
				switch (con["targetType"].AsString())
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

				_connections[key].Set(con["label"].AsString(), source, target);
			}

			foreach (string key in _boardsDict.Keys)
			{
				Dictionary board = _boardsDict[key].AsGodotDictionary();
				if (board.ContainsKey("children"))
				{
					continue;
				}
				Array<Element> boardElements = new();
				foreach (string elementKey in board["elements"].AsStringArray())
				{
					boardElements.Add(_elements[elementKey]);
				}

				Array<Jumper> boardJumpers = new();
				foreach (string jumperKey in board["jumpers"].AsStringArray())
				{
					boardJumpers.Add(_jumpers[jumperKey]);
				}

				Array<Branch> boardBranches = new();
				foreach (string branchKey in board["branches"].AsStringArray())
				{
					boardBranches.Add(_branches[branchKey]);
				}

				Array<Connection> boardConnections = new();
				foreach (string connectionKey in board["connections"].AsStringArray())
				{
					boardConnections.Add(_connections[connectionKey]);
				}

				Array<Note> boardNotes = new();
				foreach (string noteKey in board["notes"].AsStringArray())
				{
					Dictionary note = _notesDict[noteKey].AsGodotDictionary();
					_notes[noteKey] = new Note(Interpreter.Utils.CleanString(note["content"].AsString()), note["theme"].AsString());
					boardNotes.Add(_notes[noteKey]);
				}

				string customId = board.ContainsKey("customId") ? board["customId"].AsString() : null;

				_boards[key] = new Board(key, board["name"].AsString(), customId, boardElements, boardConnections, boardJumpers, boardBranches, boardNotes);
			}

			foreach (string key in _variablesDict.Keys)
			{
				Dictionary variable = _variablesDict[key].AsGodotDictionary();
				if (variable.ContainsKey("children")) continue;
				Variant value = default;
				string type = variable["type"].AsString();
				switch (type)
				{
					case "integer":
						value = variable["value"].AsInt32();
						break;
					case "float":
						value = variable["value"].AsDouble();
						break;
					case "string":
						value = variable["value"].AsString();
						break;
					case "boolean":
						value = variable["value"].AsBool();
						break;
					default:
						GD.Print("[Arcweave] Variable type \"" + variable["type"].AsString() + "\" not found");
						break;
				}
				_variables.Add(new Variable(variable["name"].AsString(), value));
				
			}

			project.Set(_startingElement, _boards, _components, _variables, _elements, _assets);
			return project;
		}

		public void MakeAssets(Array<string> assetIds, string path)
		{
			foreach (var assetId in assetIds)
			{
				var asset = _assetsDict[assetId].AsGodotDictionary();
				if (asset.ContainsKey("children") && asset["children"].VariantType != Variant.Type.Nil)
				{
					string assetName = asset.ContainsKey("root") && asset["root"].AsBool()
						? ""
						: asset["name"].AsString();
					MakeAssets(asset["children"].AsGodotArray<string>(), path + "/" + assetName);
				}
				else
				{
					string assetType = asset["type"].AsString();
					Asset.AssetType type = assetType switch
					{
						"image" or "template-image" => Asset.AssetType.Image,
						"audio" or "template-audio" => Asset.AssetType.Audio,
						_ => Asset.AssetType.Undefined
					};
					string assetName = asset["name"].AsString();
					_assets[assetId] = new Asset(assetId, assetName, path + '/' + assetName, type);
				}
			}
		}
	}
}
