WorkshopPaper.png and WorkshopInsetPaper.png are unchanged copies of the project's licensed Humble Gift - Paper UI System v1.1 assets:

- Sprites/Paper UI Pack/Plain/1 Paper/1.png
- Both paper aliases use the full plain sheet at that path. Thin edge-only variants are intentionally excluded.

CodeWorkshopUI loads these textures, creates bordered sprites for sliced uGUI panels, and uses point filtering. The source pack remains in Assets/Sprites. These copies let player builds load the workshop theme through Resources without Editor-only paths or importer dependencies. The root CanvasGroup continues to honor SettingsUiVisibility.

WorkshopReveal contains all 44 original frames from Sprites/Content Appear Animation/Folding & Cutout/7 Dialogue Box, copied in reverse numeric order (44 down to 1) to play the paper's opening reveal. WorkshopPaperAnimation plays the actual exported frames in unscaled time and holds the fully open sheet. Child content appears toward the end of the reveal. Settings pause the reveal and retain normal parent CanvasGroup visibility. Topic and difficulty changes replay the two lesson pages.
