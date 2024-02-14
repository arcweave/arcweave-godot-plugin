using System.Linq;
using Godot.Collections;
using Arcweave.Interpreter;
using Arcweave.Interpreter.INodes;
using Godot;

namespace Arcweave.Project
{
    public partial class Element
    {
        [Export] public int Visits { get; set; }

        [Export] public string Id { get; private set; }

        [Export] public Project Project { get; private set; }
        [Export] public Asset Cover { get; private set; }
        [Export] public Array<Connection> Outputs { get; private set; }

        [Export] public Array<Attribute> Attributes { get; private set; }
        [Export] public Array<Component> Components { get; private set; }
        [Export] public string Title { get; private set; }
        /// <summary>
        /// The raw content (including any html) of the element
        /// </summary>
        [Export] public string RawContent { get; private set; }
        /// <summary>
        /// The runtime content after the element's script is run
        /// </summary>
        [Export] public string RuntimeContent { get; private set; }
        public Element(string id, string title, string rawContent, Project project, Array<Connection> outputs, Array<Component> components, Array<Attribute> attributes, Asset cover)
        {
            Visits = 0;
            Id = id;
            Title = title;
            RawContent = rawContent;
            Project = project;
            Outputs = outputs;
            Components = components;
            Attributes = attributes;
            Cover = cover;
        }

        /// <summary>
        /// Adds an output to the element
        /// </summary>
        /// <param name="connection">The connection to add</param>
        public void AddOutput(Connection connection)
        {
            Outputs.Add(connection);
        }

        /// <summary>
        /// Adds an attribute to the element
        /// </summary>
        /// <param name="attribute">The attribute to add</param>
        public void AddAttribute(Attribute attribute) {  Attributes.Add(attribute); }

        /// <summary>
        /// Runs the content script of the element. This will also update
        /// the RuntimeContent of the Element.
        /// </summary>
        public void RunContentScript()
        {
            AwInterpreter i = new AwInterpreter(Project, Id);
            var output = i.RunScript(RawContent);
            if ( output.Changes.Count > 0 ) {
                foreach ( var change in output.Changes ) {
                    Project.SetVariable(change.Key, change.Value);
                }
            }

            RuntimeContent = output.Output;
        }

        Path INode.ResolvePath(Path path)
        {
            if (string.IsNullOrEmpty(path.label)) { path.label = Title; }
            path.TargetElement = this;
            return path;
        }

        /// <summary>
        /// Returns the options of the current element.
        /// </summary>
        /// <returns></returns>
        public Options GetOptions()
        {
            return new Options(this);
        }

        /// <summary>
        /// Returns a component from the Component List of th element based
        /// on it's name.
        /// </summary>
        /// <param name="componentName">The Component's name</param>
        /// <returns>Returns the component or null if not found</returns>
        public Component GetComponent(string componentName)
        {
            try
            {
                return Components.First(component => component.Name == componentName);
            }
            catch (System.InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Returns an attribute from the Attribute List of the element
        /// based on it's name.
        /// </summary>
        /// <param name="attributeName">The attribute's name</param>
        /// <returns>The attribute or null if not found</returns>
        public Attribute GetAttribute(string attributeName)
        {
            try
            {
                return Attributes.First(attribute => attribute.Name == attributeName);
            }
            catch (System.InvalidOperationException)
            {
                return null;
            }
        }
    }
}
