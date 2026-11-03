using System;

/// <summary>Safe, authored experiments. Changes demonstration values, never quiz answers or progress.</summary>
public static class BeginnerWorkshopExperiment
{
    public const int MaxExtraCoins=6;
    public static readonly string[] Messages={"Welcome, hero!","Lantern ready!","Bridge repaired!"};
    public static VisualCodeLessons.Lesson Create(string language,int topic,int message,int extraCoins)
    {
        if(topic<0||topic>1)throw new ArgumentOutOfRangeException(nameof(topic));
        var lesson=VisualCodeLessons.Get(language,topic);
        bool python=language=="python";
        Func<string,string> print=value=>python?"print("+value+")":(language=="csharp"?"Console.WriteLine(":"System.out.println(")+value+");";
        if(topic==0)
        {
            string text=Messages[Math.Max(0,Math.Min(Messages.Length-1,message))];
            lesson.code=print("\""+text+"\"")+"\n"+print("\"Begin your quest.\"");
            lesson.steps=new[]{
                new VisualCodeLessons.Step(-1,"Nothing is shown yet. Step runs one instruction. You can also change the first message with the button opposite.","",-1,"Messages\nempty"),
                new VisualCodeLessons.Step(0,"The quoted words appear as a message. The quotation marks are part of the code; they are not shown.",text,0,"Messages\n1 line"),
                new VisualCodeLessons.Step(1,"The second instruction adds another line. Change message to try different words, or Replay to watch again.",text+"\nBegin your quest.",0,"Messages\n2 lines")};
        }
        else
        {
            int start=2+Math.Max(0,Math.Min(MaxExtraCoins,extraCoins)),end=start+3;
            lesson.code=(python?"coins = "+start:"int coins = "+start+";")+"\n"+(python?"coins = coins + 3":"coins = coins + 3;")+"\n"+print("coins");
            lesson.steps=new[]{
                new VisualCodeLessons.Step(-1,"The box is empty. Step runs one instruction. Add a starting coin to see how changing the input changes the answer.","",-1,"coins\nnot set"),
                new VisualCodeLessons.Step(0,"Put "+start+" in the box named coins.","",0,"coins\n"+start),
                new VisualCodeLessons.Step(1,"Read "+start+", add 3, then replace the old value with "+end+".","",0,"coins\n"+end),
                new VisualCodeLessons.Step(2,"Print shows "+end+". Changing the starting coins changes the result; it does not give your hero gold.",end.ToString(),0,"coins\n"+end)};
        }
        return lesson;
    }
}
