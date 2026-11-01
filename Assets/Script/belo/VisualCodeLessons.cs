using System;

/// <summary>Authored traces, not a code interpreter. Each prediction uses a new example.</summary>
public static class VisualCodeLessons
{
    public sealed class Step
    {
        public int line, activeCell;
        public string explanation, output;
        public string[] cells;
        public Step(int line, string explanation, string output, int activeCell, params string[] cells)
        { this.line=line; this.explanation=explanation; this.output=output; this.activeCell=activeCell; this.cells=cells; }
    }
    public sealed class Lesson
    {
        public string topic, title, meaning, code, challenge, explanation;
        public string[] answers;
        public int correct;
        public Step[] steps;
    }
    public static readonly string[] Topics = { "output", "variables", "conditionals", "loops", "arrays", "methods" };
    public static Lesson Get(string language, int topic)
    {
        bool py=language=="python", cs=language=="csharp";
        Func<string,string> print=value=>py?"print("+value+")":(cs?"Console.WriteLine(":"System.out.println(")+value+");";
        var l=new Lesson { topic=Topics[topic], correct=0 };
        switch(topic)
        {
            case 0:
                l.title="The message sheet · Output";
                l.meaning="Output means showing a message or an answer. The console is the message area below the picture. Put words inside double quotes, like \"Hello\". The computer shows Hello without the quotes. "+(py?"print(...) shows what is inside ( ).":cs?"Console.WriteLine(...) shows what is inside ( ).":"System.out.println(...) shows what is inside ( ).");
                l.code=print("\"Welcome, hero!\"")+"\n"+print("\"Begin your quest.\"");
                l.steps=new[]{new Step(-1,"No message is shown yet. Press Step > to run the first line of code.","",-1,"Messages\nempty"),new Step(0,"The first print instruction shows Welcome, hero! in the message area.","Welcome, hero!",0,"Messages\n1 line"),new Step(1,"The next print instruction adds Begin your quest. below the first message. Both messages stay visible.","Welcome, hero!\nBegin your quest.",0,"Messages\n2 lines")};
                l.challenge=print("\"Torch lit\"")+"\n"+print("\"Gate open\"");
                l.answers=new[]{"Torch lit\nGate open","Gate open\nTorch lit","\"Torch lit\"\n\"Gate open\""};
                l.explanation="Read the print instructions from top to bottom. Torch lit appears first, then Gate open on the next line. Do not include the quotes in your answer.";
                break;
            case 1:
                l.title="The coin chest · Variables";
                l.meaning="A variable is like a labelled box that holds a value. Here the label is coins. The = sign means store the value on the right in that box. coins + 3 means take the number in coins and add 3.";
                l.code=(py?"coins = 2":"int coins = 2;")+"\n"+(py?"coins = coins + 3":"coins = coins + 3;")+"\n"+print("coins");
                l.steps=new[]{new Step(-1,"The coins box is empty. Press Step > to put the starting number into it.","",-1,"coins\nnot set"),new Step(0,"Store 2 in the chest named coins.","",0,"coins\n2"),new Step(1,"Read the old 2, add 3, then replace the chest's value with 5.","",0,"coins\n5"),new Step(2,"Print the value in coins, not its name.","5",0,"coins\n5")};
                l.challenge=(py?"coins = 4\ncoins = coins + 2":"int coins = 4;\ncoins = coins + 2;")+"\n"+print("coins");
                l.answers=new[]{"6","4","coins"};l.explanation="Work out the right side first: 4 + 2 is 6. That becomes the new value of coins.";break;
            case 2:
                l.title="The guarded gate · Conditions";
                l.meaning="if asks a yes-or-no question. keys >= 1 asks: do we have at least one key? If yes, show Open. If no, else tells us to show Locked. Only one of these messages is shown.";
                l.code=py?"keys = 1\nif keys >= 1:\n    print(\"Open\")\nelse:\n    print(\"Locked\")":(cs?"int keys = 1;":"int keys = 1;")+"\nif (keys >= 1) {\n    "+print("\"Open\"")+"\n} else {\n    "+print("\"Locked\"")+"\n}";
                l.steps=new[]{new Step(-1,"The gate awaits a decision.","",-1,"keys\nnot set","Gate\nclosed"),new Step(0,"The hero has one key.","",0,"keys\n1","Gate\nclosed"),new Step(1,"We have 1 key. Is that at least 1? Yes! Run the instructions under if.","",1,"keys\n1","Test\nTRUE"),new Step(2,"Show Open. We already chose the yes path, so do not run the instructions under else.","Open",1,"keys\n1","Gate\nOPEN")};
                l.challenge=py?"keys = 0\nif keys >= 1:\n    print(\"Open\")\nelse:\n    print(\"Locked\")":"int keys = 0;\nif (keys >= 1) {\n    "+print("\"Open\"")+"\n} else {\n    "+print("\"Locked\"")+"\n}";
                l.answers=new[]{"Locked","Open","Open\nLocked"};l.explanation="0 >= 1 is false. Use the instructions under else, so the gate stays locked.";break;
            case 3:
                l.title="The training trail · Loops";
                l.meaning=py?"A loop repeats instructions. range(3) gives us the numbers 0, 1 and 2. The spaces before print(i) show that it belongs to the loop. Print once for each number, then stop.":"A loop repeats instructions. i is a counter: it starts at 0. i < 3 means keep going while i is below 3. i++ adds 1 after each repeat. The instructions inside { } run for 0, 1 and 2.";
                l.code=py?"for i in range(3):\n    print(i)":"for (int i = 0; i < 3; i++) {\n    "+print("i")+"\n}";
                l.steps=new[]{new Step(-1,"We will repeat the print instruction three times. Start counting at 0 and stop before 3.","",-1,"pass 1\nwaiting","pass 2\nwaiting","pass 3\nwaiting"),new Step(1,"First repeat: the counter named i holds 0. Show 0 in the message area.","0",0,"i = 0\nvisited","pass 2\nwaiting","pass 3\nwaiting"),new Step(1,"Second repeat: i is now 1. Add 1 on the next message line.","0\n1",1,"i = 0\nvisited","i = 1\nvisited","pass 3\nwaiting"),new Step(1,"Third repeat: i is now 2. Add 2 on the next message line.","0\n1\n2",2,"i = 0\nvisited","i = 1\nvisited","i = 2\nvisited"),new Step(0,"The counter has reached the stopping point, 3. Stop repeating. The number 3 is not printed.","0\n1\n2",-1,"i = 0\nvisited","i = 1\nvisited","i = 2\nvisited")};
                l.challenge=py?"for i in range(2):\n    print(\"Step\")":"for (int i = 0; i < 2; i++) {\n    "+print("\"Step\"")+"\n}";
                l.answers=new[]{"Step\nStep","Step","Step\nStep\nStep"};l.explanation="The counter takes two values, 0 and 1. The repeated print instruction shows Step twice.";break;
            case 4:
                l.title="The potion shelf · "+(py?"Lists":"Arrays");
                l.meaning=py?"A list is like a row of numbered shelf spaces. The first space is numbered 0, the next is 1, then 2. This position number is called an index. potions[1] means read the value in space 1: the second space.":"An array is like a row of numbered shelf spaces. The first space is numbered 0, the next is 1, then 2. This position number is called an index. potions[1] means read the value in space 1: the second space.";
                l.code=(py?"potions = [4, 7, 9]":"int[] potions = {4, 7, 9};")+"\n"+print("potions[1]");
                l.steps=new[]{new Step(-1,"An empty shelf will hold three values.","",-1,"index 0\nempty","index 1\nempty","index 2\nempty"),new Step(0,"Fill the shelf in order. The first position is index 0.","",-1,"index 0\n4","index 1\n7","index 2\n9"),new Step(1,"Select index 1, the second cell. Print 7.","7",1,"index 0\n4","index 1\n7","index 2\n9")};
                l.challenge=(py?"potions = [3, 6, 8]":"int[] potions = {3, 6, 8};")+"\n"+print("potions[0]");
                l.answers=new[]{"3","6","0"};l.explanation="Index 0 refers to the first cell, whose stored value is 3. The index and the value are different things.";break;
            default:
                l.title="The healing workshop · "+(py?"Functions":"Methods");
                l.meaning="A "+(py?"function":"method")+" is a named set of instructions you can use again. To use it, write its name with a number in ( ). That number is the input. Here hp holds the input. return hp + 2 sends back the input plus 2.";
                l.code=py?"def heal(hp):\n    return hp + 2\nprint(heal(3))":(cs?"static int Heal(int hp) {":"static int heal(int hp) {")+"\n    return hp + 2;\n}\n"+print(cs?"Heal(3)":"heal(3)");
                l.steps=new[]{new Step(-1,"Writing the healing instructions does not use them yet. Look for the line that uses their name.","",-1,"input\n3","hp\nnot set","result\nwaiting"),new Step(py?2:3,"Use the healing instructions with input 3. Put that 3 into the box named hp.","",1,"input\n3","hp\n3","result\nwaiting"),new Step(1,"Add 2 to the number in hp: 3 + 2 is 5. Give back 5 as the answer.","",2,"input\n3","hp\n3","result\n5"),new Step(py?2:3,"The healing instructions give back 5. The print instruction shows that answer.","5",2,"input\n3","hp\n3","result\n5")};
                l.challenge=py?"def heal(hp):\n    return hp + 2\nprint(heal(6))":(cs?"static int Heal(int hp) {":"static int heal(int hp) {")+"\n    return hp + 2;\n}\n"+print(cs?"Heal(6)":"heal(6)");
                l.answers=new[]{"8","6","2"};l.explanation="Start with input 6. Store it in hp. Add 2 to get 8. The healing instructions give back 8, and the print instruction shows it.";break;
        }
        // Keep the correct choice from always occupying the first button.
        l.correct=topic%3;
        string first=l.answers[0];l.answers[0]=l.answers[l.correct];l.answers[l.correct]=first;
        return l;
    }
}


