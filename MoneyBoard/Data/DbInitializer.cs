using MoneyBoard.Models;

namespace MoneyBoard.Data
{
    public class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // 既にデータが存在する場合は初期化しない
            if (!context.Database.EnsureCreated())
            {
                return; // DB has been seeded
            }

            #region Categories

            var categories = new Category[]
            {
                new Category {
                    //Id = 1,
                    Name = "食費",
                    ColorHex = "#FF6B6B" // 赤系 - 食欲をそそる暖色
                },
                new Category {
                    //Id = 2,
                    Name = "コンビニ",
                    ColorHex = "#FF9500" // オレンジ - 手軽さ・利便性
                },
                new Category {
                    //Id = 3,
                    Name = "住居費",
                    ColorHex = "#4ECDC4" // ターコイズ - 安定感のある色
                },
                new Category {
                    //Id = 4,
                    Name = "光熱費",
                    ColorHex = "#FFD93D" // 黄色 - エネルギーを連想
                },
                new Category {
                    //Id = 5,
                    Name = "交通費",
                    ColorHex = "#6BCF7F" // 緑 - 移動のイメージ
                },
                new Category {
                    //Id = 6,
                    Name = "通信費",
                    ColorHex = "#95E1D3" // ミントグリーン - 通信の爽やかさ
                },
                new Category {
                    //Id = 7,
                    Name = "医療費",
                    ColorHex = "#E63946" // 深紅 - 医療・健康の重要性
                },
                new Category {
                    //Id = 8,
                    Name = "衣服費",
                    ColorHex = "#B565D8" // 紫 - ファッション性
                },
                new Category {
                    //Id = 9,
                    Name = "娯楽費",
                    ColorHex = "#FF9FF3" // ピンク - 楽しさ
                },
                new Category {
                    //Id = 12,
                    Name = "交際費",
                    ColorHex = "#FFB6C1" // ライトピンク - 人間関係
                },
                new Category {
                    //Id = 13,
                    Name = "美容費",
                    ColorHex = "#DDA0DD" // プラム - 美しさ
                },
                new Category {
                    //Id = 15,
                    Name = "日用品",
                    ColorHex = "#87CEEB" // スカイブルー - 日常性
                },
                new Category {
                    //Id = 16,
                    Name = "その他",
                    ColorHex = "#A9A9A9" // ダークグレー（元データから推測）
                }
            };
            foreach (var c in categories)
            {
                context.Categories.Add(c);
            }
            context.SaveChanges();

            #endregion
        }
    }
}
