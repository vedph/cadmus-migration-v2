using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Cadmus.Export;

/// <summary>
/// Base class to use with POCO options classes requiring to define one or
/// more XML namespaces, to be resolved during processing like with
/// <see cref="XPathNodeIterator"/> or when it is otherwise required to
/// lookup namespaces by their prefix (e.g.
/// <see cref="XmlNamespaceManager.LookupNamespace(string)"/>) or vice-versa
/// (<see cref="XmlNamespaceManager.LookupPrefix(string)"/>).
/// </summary>
public class NamespaceOptions
{
    /// <summary>
    /// The XML namespace.
    /// </summary>
    public readonly static XNamespace XML = "http://www.w3.org/XML/1998/namespace";

    /// <summary>
    /// The TEI namespace.
    /// </summary>
    public readonly static XNamespace TEI = "http://www.tei-c.org/ns/1.0";

    /// <summary>
    /// Gets or sets the prefix to use for the default namespace. When a
    /// document has a default namespace, and thus an empty prefix, we may
    /// still require a prefix, e.g. to query it via XPath. So if you are
    /// going to use XPath and you have a document with default namespace,
    /// set its prefix via this option.
    /// </summary>
    public string? DefaultNsPrefix { get; set; }

    /// <summary>
    /// Gets or sets the optional list of namespace prefixes and values
    /// in the form <c>prefix=namespace</c>, like e.g.
    /// <c>tei=http://www.tei-c.org/ns/1.0</c>.
    /// </summary>
    public IList<string>? Namespaces { get; set; }

    /// <summary>
    /// Gets the colonized <see cref="DefaultNsPrefix"/> or null.
    /// </summary>
    /// <returns>The value of <see cref="DefaultNsPrefix"/> ending with
    /// a colon, or null.</returns>
    public string? GetColonizedDefaultNsPrefix()
    {
        if (DefaultNsPrefix?.EndsWith(":", StringComparison.Ordinal) == true)
        {
            return DefaultNsPrefix;
        }
        else if (DefaultNsPrefix != null)
        {
            return DefaultNsPrefix + ":";
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Gets an XML namespace manager using the namespaces defined in
    /// <see cref="Namespaces"/>.
    /// </summary>
    /// <param name="xml">if set to <c>true</c>, add the XML namespace when
    /// no entry with prefix <c>xml</c> is found in <see cref="Namespaces"/>.
    /// </param>
    /// <param name="tei">if set to <c>true</c>, add the TEI namespace when
    /// no entry with prefix <c>tei</c> is found in <see cref="Namespaces"/>.
    /// </param>
    /// <param name="table">The table.</param>
    /// <returns>Manager.</returns>
    public IXmlNamespaceResolver GetResolver(bool xml = true, bool tei= true,
        NameTable? table = null)
    {
        XmlNamespaceManager nsmgr = new(table ?? new NameTable());

        if (xml && Namespaces?.Any(ns => ns.StartsWith("xml=")) != true)
            nsmgr.AddNamespace("xml", XML.NamespaceName);

        if (tei && Namespaces?.Any(ns => ns.StartsWith("tei=")) != true)
            nsmgr.AddNamespace("tei", TEI.NamespaceName);

        if (Namespaces?.Count > 0)
        {
            foreach (string ns in Namespaces)
            {
                int i = ns.IndexOf('=');
                if (i > -1 && i + 1 < ns.Length)
                    nsmgr.AddNamespace(ns[..i], ns[(i + 1)..]);
            }
        }

        return nsmgr;
    }

    /// <summary>
    /// Converts into <see cref="XName"/> a string where a fully qualified
    /// XML name is represented by an arbitrary namespace prefix plus
    /// <c>:</c> followed by a local name (e.g. <c>tei:div</c>), using
    /// the specified namespace resolver.
    /// If the name has no prefix, it just belongs to the default namespace.
    /// </summary>
    /// <param name="name">The prefixed name.</param>
    /// <param name="resolver">The namespace resolver.</param>
    /// <returns>XML name.</returns>
    /// <param name="defaultNsPrefix">The optional default namespace prefix
    /// to use when no prefix is found in <paramref name="name"/>.</param>
    /// <exception cref="ArgumentNullException">name or resolver</exception>
    public static XName PrefixedNameToXName(string name,
        IXmlNamespaceResolver resolver,
        string? defaultNsPrefix = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(resolver);

        string? ns;
        int i = name.IndexOf(':');
        if (i == -1)
        {
            if (defaultNsPrefix == null) return name;
            ns = resolver.LookupNamespace(defaultNsPrefix);
            return ns != null ? XName.Get(name, ns) : name;
        }
        else
        {
            if (i + 1 == name.Length) return name[0..i];
            ns = resolver.LookupNamespace(name[..i]);
            if (ns == null) return name[(i + 1)..];
            return XName.Get(name[(i + 1)..], ns);
        }
    }
}
