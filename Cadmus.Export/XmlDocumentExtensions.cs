using System.Xml.Linq;
using System.Xml;

// TODO: remove when using the same extensions from newer Fusi.Xml
// https://stackoverflow.com/questions/1508572/converting-xdocument-to-xmldocument-and-vice-versa

namespace Cadmus.Export;

/// <summary>
/// Extensions for conversion between <see cref="XDocument"/> and
/// <see cref="XmlDocument"/>.
/// </summary>
public static class XmlDocumentExtensions
{
    /// <summary>
    /// Converts <see cref="XDocument"/> to <see cref="XmlDocument"/>.
    /// </summary>
    /// <param name="doc">The document.</param>
    /// <returns>Document.</returns>
    public static XmlDocument ToXmlDocument(this XDocument doc)
    {
        XmlDocument xmlDoc = new();
        using (XmlReader xmlReader = doc.CreateReader())
        {
            xmlDoc.Load(xmlReader);
        }
        return xmlDoc;
    }

    /// <summary>
    /// Converts <see cref="XmlDocument"/> to <see cref="XDocument"/>.
    /// </summary>
    /// <param name="doc">The document.</param>
    /// <returns>Document.</returns>
    public static XDocument ToXDocument(this XmlDocument doc)
    {
        using XmlNodeReader nodeReader = new(doc);
        nodeReader.MoveToContent();
        return XDocument.Load(nodeReader);
    }
}
