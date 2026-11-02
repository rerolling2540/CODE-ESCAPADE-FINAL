# Battle report scroll

These PNG parts are unchanged copies of Assets/Sprites/Scroll/pngs/parts (upper, lower, paper and ribbon). The supplied parts are assembled and animated by BattleReportScroll: the paper unrolls between moving rollers, waits for Continue, and rolls closed using unscaled time. The source asset license is retained in Assets/Sprites/Scroll/License.txt. The ribbon copy is reserved for future decoration and is not placed over report text.

QuestionSystem snapshots each presented battle question and records selected/correct answers, topic, outcome and elapsed time. Configure resets the encounter session, so the report never combines earlier battles or other save slots. Accuracy is correct / (correct + wrong + timed out); unfinished questions are shown separately and excluded. Answered means correct + wrong. No resolved attempts displays N/A.

Victory waits for the report before the existing return routine. Defeat shows it after the death animation and before respawn choices. XP award and save rules are unchanged. This report is a per-battle review, not a new cloud table or persistent report archive. Retreats do not open this report. The existing performance tracker continues to record learning history independently.
