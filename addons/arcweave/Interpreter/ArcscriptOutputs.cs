#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Arcweave.Interpreter
{
    public interface IOutputNode
    {
        public string GetText();
        public void MergeScriptOutput(string text);
    }

    public interface IHasParagraphs
    {
        public void AppendParagraph(string text);
    }

    public class Paragraph : IOutputNode
    {
        private string _text;

        public Paragraph(string text)
        {
            _text = text;
        }

        public void MergeScriptOutput(string text)
        {
            if (text.Length > 0)
            {
                if (_text.Length > 0)
                {
                    this._text += ' ' + text;                    
                }
                else
                {
                    this._text = text;
                }
                
            }
        }

        public string GetText()
        {
            return "<p>" + _text + "</p>";
        }
    }

    public class Blockquote : IHasParagraphs, IOutputNode
    {
        public List<Paragraph> Paragraphs { get; set; } = new();

        public void AppendParagraph(string text)
        {
            Paragraphs.Add(new Paragraph(text));
        }
        
        public void MergeScriptOutput(string text)
        {
            if (Paragraphs.Count == 0)
            {
                AppendParagraph(text);
                return;
            }
            
            Paragraphs.Last().MergeScriptOutput(text);
        }

        public string GetText()
        {
            var output = "";
            foreach (var p in Paragraphs)
            {
                output += p.GetText();
            }

            return "<blockquote>" + output + "</blockquote>";
        }
    }

    public class ArcscriptOutputs : IHasParagraphs
    {
        private List<IOutputNode> Outputs { get; } = new();
        private IHasParagraphs? _currentNode;
        private bool _addedScript;

        public void AppendParagraph(string text)
        {
            Outputs.Add(new Paragraph(text));
        }
        
        public void AddParagraph(string text)
        {
            _currentNode ??= this;

            if (_addedScript)
            {
                if (Outputs.Count > 0 && Outputs.Last() is Blockquote && _currentNode is not Blockquote)
                {
                    AppendParagraph(text);
                }
                else
                {
                    AddScriptOutput(text);
                }
                _addedScript = false;
                return;
            }
            _currentNode.AppendParagraph(text);
        }

        public void AddBlockquote()
        {
            if (_addedScript && Outputs.Count > 0)
            {
                IOutputNode n = Outputs.Last();
                if (n is Blockquote blockquote)
                {
                    _currentNode = blockquote;
                    return;
                }
            }
            Blockquote b = new Blockquote();
            Outputs.Add(b);
            _currentNode = b;
        }

        public void AddScriptOutput(string? text)
        {
            _addedScript = true;
            if (text == null)
            {
                return;
            }
            if (Outputs.Count == 0)
            {
                Outputs.Add(new Paragraph(text));
                return;
            }

            var n = Outputs.Last();
            n.MergeScriptOutput(text);
        }

        public void ExitBlockquote()
        {
            _currentNode = this;
        }

        public string GetText()
        {
            var output = "";
            foreach (var o in Outputs)
            {
                output += o.GetText();
            }

            return output;
        }
    }
}

