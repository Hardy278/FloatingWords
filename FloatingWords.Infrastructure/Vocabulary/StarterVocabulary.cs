using FloatingWords.Domain.Vocabulary;

namespace FloatingWords.Infrastructure.Vocabulary;

/// <summary>
/// 首次启动写入数据库的内置示例内容，不生成外部词书文件。
/// </summary>
public static class StarterVocabulary
{
    public const string ListId = "starter";

    public static VocabularyList Create() => new(
        ListId,
        "浮光词集",
        "en",
        [
            new VocabularyEntry(
                "float",
                "float",
                new Phonetic("/fləʊt/", "/floʊt/"),
                [
                    new VocabularySense(
                        "float_v_1",
                        "v.",
                        ["核心"],
                        [
                            new Definition(
                                "float_v_1_def_1",
                                "to stay on or near the surface of a liquid",
                                "漂浮；浮动")
                        ],
                        [
                            new Example(
                                "float_v_1_ex_1",
                                "The leaf floated on the water.",
                                "叶子漂浮在水面上。")
                        ]),
                    new VocabularySense(
                        "float_n_1",
                        "n.",
                        ["核心"],
                        [
                            new Definition(
                                "float_n_1_def_1",
                                "a thing that floats on water",
                                "浮标；漂浮物")
                        ],
                        [])
                ]),
            new VocabularyEntry(
                "serendipity",
                "serendipity",
                new Phonetic("/ˌserənˈdɪpəti/", "/ˌserənˈdɪpəti/"),
                [
                    new VocabularySense(
                        "serendipity_n_1",
                        "n.",
                        ["进阶"],
                        [
                            new Definition(
                                "serendipity_n_1_def_1",
                                "the fact of discovering something pleasant by chance",
                                "意外发现美好事物的机缘")
                        ],
                        [
                            new Example(
                                "serendipity_n_1_ex_1",
                                "Finding that book was pure serendipity.",
                                "找到那本书纯属机缘巧合。")
                        ])
                ]),
            new VocabularyEntry(
                "current",
                "current",
                new Phonetic("/ˈkʌrənt/", "/ˈkɜːrənt/"),
                [
                    new VocabularySense(
                        "current_adj_1",
                        "adj.",
                        ["高频"],
                        [
                            new Definition(
                                "current_adj_1_def_1",
                                "happening or existing now",
                                "当前的；现行的")
                        ],
                        [
                            new Example(
                                "current_adj_1_ex_1",
                                "The current situation is stable.",
                                "目前形势稳定。")
                        ]),
                    new VocabularySense(
                        "current_n_1",
                        "n.",
                        ["高频"],
                        [
                            new Definition(
                                "current_n_1_def_1",
                                "a continuous flow of water, air, or electricity",
                                "水流；电流；趋势")
                        ],
                        [])
                ])
        ]);
}
