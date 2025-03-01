# Rendering Sample - Plain Text Output

This sample configuration exports a set of Cadmus text items (ignoring any layers) into a set of plain text files, to allow third party tools further process the resulting text.

## Requirements

Here the sample tool is a Chiron-based linguistic analyzer for prose rhythm, having as input a set of plain text files with the text to be analyzed. This text is preceded by a metadata header, where each metadatum is in a single line starting with `.` and having a name followed by `=` and its value. For instance, this is a document from _Constitutiones Sirmondianae_:

```txt
.date=333 Mai. 5
.date-value=333
.data=Dat. III nonas Maias Constantinopoli Dalmatio et Zenofilo conss.
.title=01 Imp(erator) Constantinus A(ugustus) ad Ablabium pp. 
Satis mīrātī sumus gravitātem tuam, quae plēna iūstitiae ac probae religiōnis est, clēmentiam nostram scīscitārī voluisse, quid dē sententiīs epīscopōrum vel ante moderātiō nostra cēnsuerit vel nunc servārī cupiāmus, Ablābī, parēns kārissime atque amantissime.
...
```

The analyzer has no other requirement for its input format. Yet, for other processing types, it might be useful to optionally pre-segment the text into sentences. For instance, this happens when dealing with NLP tokenizers in conjunction with Chiron-based linguistic analysis. In this case, we can apply a simple sentence splitter filter, which refactors the text layout to ensure that each line corresponds to a single sentence.

## Data Architecture

In our scenario, Cadmus text items have a facet equal to `text`, and use a `TokenTextPart` for the text. They also use layer parts, like critical apparatus; but here we are just interested in exporting raw text.

As usual in Cadmus, the text is just a set of items, where each part contains a paragraph or a poetical composition cited in the context of a document. This ensures that every item stands on its own, and can get the required layers. These text portions are virtually grouped under each "work" by means of item group IDs.

The text being edited in Cadmus in this sample is _Sidonius Apollinaris_ letters. Their text is split into items at each paragraph or poetical composition, and each chunk of text belongs to a letter via its group ID, which has the form `N-NNN` where `N` is the book number (1-9) and `NNN` is the letter number in that book.

For instance, `1-002` is the second letter of the first book. This ensures that each text item contains only prose or poetry, and never a mix of the two. Poetic items are marked by a flag value of 8 (and by a final asterisk in their title).

So, we want to extract the raw text from each of these chunks, in their order, and create a new file for each letter. Also, we want some text preprocessing. For instance, many letters end with the salutation `vale`, like e.g. 1.1:

```txt
... sed si et hisce deliramentis genuinum molarem invidia non fixerit, actutum tibi a nobis volumina numerosiora percopiosis scaturrientia sermocinationibus multiplicabuntur. vale.
```

As we are going to analyze prose rhythm, such salutations would introduce rumor in our analysis data. So, we want to remove them during export.

Also, the apostrophe character is used as a quote marker in these texts; so we want to replace `'` with `"`, to produce a text more compliant to the underlying character semantics defined by the Unicode standard, and honored in the linguistic analyzer.

Finally, if we are going to split text into sentences, it will be useful to move sentence-end punctuation like `.` after a quote marker, so that the sentence will not be cut leaving an orphaned closing quote.

We can easily accomplish all these preprocessing requirements using a replacement filter.

## Configuration

```json
{
  "RendererFilters": [
    {
      "Keys": "rep-filter",
      "Id": "it.vedph.renderer-filter.replace",
      "Options": {
        "Replacements": [
          {
            "Source": "([.;:?!])\\s+vale\\.[ ]*([\\r\\n]+)",
            "IsPattern": true,
            "Target": "$1$2",
            "Repetitions": 1
          },
          {
            "Source": "\\d+\\.\\s+",
            "IsPattern": true,
            "Target": "",
            "Repetitions": 1
          },
          {
            "Source": "'",
            "Target": "\"",
            "Repetitions": 1
          },
          {
            "Source": "([.?!])\"",
            "IsPattern": true,
            "Target": "\"$1"
          }
        ]
      }
    },
    {
      "Keys": "split-filter",
      "Id": "it.vedph.renderer-filter.sentence-split",
      "Options": {
        "EndMarkers": ".?!",
        "Trimming": true,
        "BlackOpeners": "(",
        "BlackClosers": ")",
        "CrLfRemoval": true
      }
    }
  ],
  "TextPartFlatteners": [
    {
      "Keys": "it.vedph.token-text",
      "Id": "it.vedph.text-flattener.token"
    }
  ],
  "TextTreeRenderers": [
    {
      "Keys": "txt",
      "Id": "it.vedph.text-tree-renderer.txt",
      "Options": {
        "FilterKeys": ["rep-filter", "split-filter"]
      }
    }
  ],
  "ItemComposers": [
    {
      "Keys": "default",
      "Id": "it.vedph.item-composer.txt.fs",
      "Options": {
        "TextPartFlattenerKey": "it.vedph.token-text",
        "TextBlockRendererKey": "txt",
        "ItemGrouping": true,
        "OutputDirectory": "c:\\users\\dfusi\\Desktop\\out",
        "TextHead": ".author=Sidonius Apollinaris\r\n.date=v AD\r\n.date-value=450\r\n.title={item-title}\r\n"
      }
    }
  ],
  "ItemIdCollector": {
    "Id": "it.vedph.item-id-collector.mongo",
    "Options": {
      "FacetId": "text",
      "Flags": 8,
      "FlagMatching": 2
    }
  }
}
```

1. a replacer renderer filter is used to remove the final `vale` and eventual artifacts represented by paragraph numbers. To this end, we use a couple of regular expressions. This filter is defined with key `rep-filter`.
2. a sentence splitting filter is used to rearrange newlines so that each line corresponds to a sentence. This facilitates the usage of the target tool.
3. a text part flattener is used to flatten the token-based text part of each text item. This part's model has a list of lines, each with its text. These lines will become rows of text blocks; in this case, given that we include no layer in the output, we will just have a single block for each row.
4. a text block renderer is used to extract blocks as plain text. Also, once extracted the text gets filtered by the `rep-filter` defined above.
5. an item composer puts all these pieces together: it is a plain text, file-based composer, using the text flattener and block renderer defined above; it applies grouping, i.e. it will change its output file whenever a new group is found; uses the specified output directory, and prepends to each file a "header" with the format explained above. This header includes metadata placeholders between curly braces. For instance, `{item-title}` will be replaced by the title of each item being processed. File names instead will be equal to group IDs.
6. an item ID collector is used to collect all the text items (facet ID = `text`) from the MongoDB database containing Sidonius Apollinaris. Notice that an additional filter here is used to exclude poetic text from the export, as we do not want to have poetic text analyzed by a prose rhythm tool. So, we are excluding from collection all the items having flag 8 set (as flag 8 represents a poetic text in this database). Property `FlagMatching=2` means that we are matching all the items NOT having the specified flags set.

The command used in the CLI is (assuming that this configuration file is named `Preview-txt` under my desktop):

```ps1
./cadmus-mig render-items cadmus-sidon C:\Users\dfusi\Desktop\Preview-txt.json
```

## Sample Output

The first file output by this configuration, without the sentence splitting filter, would be:

```txt
.author=Sidonius Apollinaris
.date=v AD
.date-value=450
.title=1_001_001 Sidonius Constantio suo salutem.
Diu praecipis, domine maior, summa suadendi auctoritate, sicuti es in his quae deliberabuntur consiliosissimus, ut, si quae litterae paulo politiores varia occasione fluxerunt, prout eas causa persona tempus elicuit, omnes retractatis exemplaribus enucleatisque uno volumine includam, Quinti Symmachi rotunditatem, Gai Plinii disciplinam maturitatemque vestigiis praesumptiosis insecuturus.
nam de Marco Tullio silere melius puto, quem in stilo epistulari nec Iulius Titianus sub nominibus illustrium feminarum digna similitudine expressit. propter quod illum ceteri quique Frontonianorum utpote consectaneum aemulati, cur veternosum dicendi genus imitaretur, oratorum simiam nuncupaverunt. quibus omnibus ego immane dictu est quantum semper iudicio meo cesserim quantumque servandam singulis pronuntiaverim temporum suorum meritorumque praerogativam.
sed scilicet tibi parui tuaeque examinationi has <litterulas> non recensendas (hoc enim parum est) sed defaecandas, ut aiunt, limandasque commisi, sciens te immodicum esse fautorem non studiorum modo verum etiam studiosorum. quam ob rem nos nunc perquam haesitabundos in hoc deinceps famae pelagus impellis.
porro autem super huiusmodi opusculo tutius conticueramus, contenti versuum felicius quam peritius editorum opinione, de qua mihi iampridem in portu iudicii publici post lividorum latratuum Scyllas enavigatas sufficientis gloriae ancora sedet. sed si et hisce deliramentis genuinum molarem invidia non fixerit, actutum tibi a nobis volumina numerosiora percopiosis scaturrientia sermocinationibus multiplicabuntur.
```

Note that here the original letter had a final `vale.` which has been removed by the filter.

By applying also sentence splitting, the result is:

```txt
.author=Sidonius Apollinaris
.date=v AD
.date-value=450
.title=1_001_001 Sidonius Constantio suo salutem.
Diu praecipis, domine maior, summa suadendi auctoritate, sicuti es in his quae deliberabuntur consiliosissimus, ut, si quae litterae paulo politiores varia occasione fluxerunt, prout eas causa persona tempus elicuit, omnes retractatis exemplaribus enucleatisque uno volumine includam, Quinti Symmachi rotunditatem, Gai Plinii disciplinam maturitatemque vestigiis praesumptiosis insecuturus.
nam de Marco Tullio silere melius puto, quem in stilo epistulari nec Iulius Titianus sub nominibus illustrium feminarum digna similitudine expressit.
propter quod illum ceteri quique Frontonianorum utpote consectaneum aemulati, cur veternosum dicendi genus imitaretur, oratorum simiam nuncupaverunt.
quibus omnibus ego immane dictu est quantum semper iudicio meo cesserim quantumque servandam singulis pronuntiaverim temporum suorum meritorumque praerogativam.
sed scilicet tibi parui tuaeque examinationi has <litterulas> non recensendas (hoc enim parum est) sed defaecandas, ut aiunt, limandasque commisi, sciens te immodicum esse fautorem non studiorum modo verum etiam studiosorum.
quam ob rem nos nunc perquam haesitabundos in hoc deinceps famae pelagus impellis.
porro autem super huiusmodi opusculo tutius conticueramus, contenti versuum felicius quam peritius editorum opinione, de qua mihi iampridem in portu iudicii publici post lividorum latratuum Scyllas enavigatas sufficientis gloriae ancora sedet.
sed si et hisce deliramentis genuinum molarem invidia non fixerit, actutum tibi a nobis volumina numerosiora percopiosis scaturrientia sermocinationibus multiplicabuntur.
```

Now every line corresponds to a single sentence.

So, in the end we have exported a set of plain text files prepared with some metadata and preprocessing so that they can be easily ingested by the target analysis system without further processing. This will ["macronize"](https://github.com/Myrmex/alatius-macronizer-api) the text, and then proceed further with its prosodical and rhythmic analysis.
