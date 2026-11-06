using System;

/// <summary>Original applied/composite examples. Tier names describe workshop complexity, not player level.</summary>
public static class WorkshopVariations
{
    public static VisualCodeLessons.Lesson Get(string lang,int topic,int tier)
    {
        var l=VisualCodeLessons.Get(lang,topic);if(tier==0)return l;
        bool py=lang=="python",cs=lang=="csharp",hard=tier==2;
        Func<string,string> p=x=>py?"print("+x+")":(cs?"Console.WriteLine(":"System.out.println(")+x+");";
        Func<string,string,string> assign=(name,value)=>py?name+" = "+value:"int "+name+" = "+value+";";
        string code,answer,reason;
        switch(topic)
        {
            case 0:
                code=hard?p("(2 + 3) * 2")+"\n"+p("\"Quest ready\""):p("2 + 3");answer=hard?"10\nQuest ready":"5";
                reason=hard?"Parentheses add 2 and 3 first. Multiplication produces 10, then a separate call prints the message.":"Without quotes, the computer adds the numbers before showing the answer. Here 2 + 3 gives 5.";break;
            case 1:
                code=assign("coins","4")+"\n"+(py?"coins = coins * 2":"coins = coins * 2;")+"\n"+(hard?(py?"coins = coins - 3":"coins = coins - 3;")+"\n":"")+p("coins");answer=hard?"5":"8";
                reason=hard?"Each assignment uses the latest value: 4 becomes 8, then 8 - 3 becomes 5.":"The right side reads the old 4, multiplies by 2, and stores 8.";break;
            case 2:
                string test=hard?(py?"keys >= 1 and coins >= 5":"keys >= 1 && coins >= 5"):"coins >= 5";
                code=(hard?assign("keys","1")+"\n":"")+assign("coins",hard?"4":"5")+"\n"+(py?"if "+test+":\n    print(\"Open\")\nelse:\n    print(\"Locked\")":"if ("+test+") {\n    "+p("\"Open\"")+"\n} else {\n    "+p("\"Locked\"")+"\n}");answer=hard?"Locked":"Open";
                reason=hard?"Both comparisons must be true. One key is enough, but 4 coins is below 5, so the gate stays locked.":"We have exactly 5 coins. Is that at least 5? Yes. Show Open and skip else.";break;
            case 3:
                code=assign("total","0")+"\n"+(py?"for i in range(1, 4):\n    total = total + "+(hard?"i":"2"):"for (int i = 1; i < 4; i++) {\n    total = total + "+(hard?"i":"2")+";\n}")+"\n"+p("total");answer="6";
                reason=hard?"The loop adds 1, then 2, then 3: total changes 0 → 1 → 3 → 6. The final print runs once after the loop.":"Three passes each add 2: total changes 0 → 2 → 4 → 6. The final print runs once after the loop.";break;
            case 4:
                code=(py?"potions = [3, 6, 8]":"int[] potions = {3, 6, 8};")+"\n"+(hard?(py?"potions[1] = potions[0] + 2":"potions[1] = potions[0] + 2;")+"\n":"")+p(hard?"potions[1]":"potions[0] + potions[2]");answer=hard?"5":"11";
                reason=hard?"Read the first cell's 3, add 2, then replace only the second cell with 5.":"Read values at indexes 0 and 2: 3 + 8 is 11. The indexes select values; they are not the values.";break;
            default:
                string name=cs?"Heal":"heal";
                code=py?"def heal(hp):\n    return hp + 2\nprint("+(hard?"heal(heal(3))":"heal(4) + heal(1)")+")":"static int "+name+"(int hp) {\n    return hp + 2;\n}\n"+p(hard?name+"("+name+"(3))":name+"(4) + "+name+"(1)");answer=hard?"7":"9";
                reason=hard?"The inner call returns 5. That result becomes the outer call's argument, so the outer call returns 7.":"The two calls are independent: healing 4 returns 6; healing 1 returns 3. Add those two answers: 6 + 3 is 9.";break;
        }
        l.challenge=code;l.answers=new[]{answer,"No output","An error occurs"};l.correct=0;l.explanation=reason;
        string[] concepts={hard?" In this exercise, * means multiply. Work out the sum inside ( ) first, then multiply. Example: (1 + 2) * 3 becomes 3 * 3. Read the print lines from top to bottom.":" In this exercise, + adds numbers. Printing 1 + 2 shows 3. Quotes show words; without quotes, the computer can calculate.",hard?" This time, * means multiply and - means subtract. Update the box one line at a time. Always use the latest number in it.":" This time, * means multiply. Multiply the number in the box, then put the answer back into the same box.",hard?(py?" This time, and joins two tests. Both must say yes to open the gate. If even one says no, use else.":" This time, && means AND. Both tests must say yes to open the gate. If even one says no, use else."):" This time, check whether there are at least 5 coins. >= includes exactly 5. Choose the yes or no path; do not run both.",hard?" This time, total is a box that starts at 0. Add the current counter number on each repeat. Keep the updated total for the next repeat. Print it only after all repeats finish.":" This time, total is a box that starts at 0. Each repeat adds a fixed amount to its latest value. After the loop ends, print the final total.",hard?" This time, = changes one shelf space. Read the value on the right, do the addition, then store the answer at the position on the left.":" This time, read two shelf spaces and add their stored numbers. Start counting positions at 0, not 1.",hard?" This time, one healing instruction is inside another. Work out the inside ( ) first. Its answer becomes the input for the outside instruction.":" This time, use the healing instructions twice, once for each input. Each use gives its own answer. Add those two answers."};
        string[] meanings={
            "Output means showing a message or a calculated answer. Quoted words are shown as text; numbers without quotes can be calculated before they are shown.",
            "A variable is a named box for a value. Assignment (=) stores the value on the right in that box. Later instructions use its latest value.",
            "A condition chooses which instructions run. if checks a yes-or-no test. When the test is false, else supplies the other path. Only one path runs.",
            "A loop repeats instructions. The counter i changes on each pass. total keeps the running sum. An instruction after the loop runs only when repetition is finished.",
            "An "+(py?"indexed list":"array")+" is a row of numbered places for values. Its first index is 0. Read a stored value with potions[index]; an assignment can replace that value.",
            "A "+(py?"function":"method")+" is a named set of reusable instructions. Each call supplies an input, and return gives its result back to the calling instruction."
        };
        l.meaning=meanings[topic]+"\n\n"+concepts[topic].Trim();
        int number;
        if(int.TryParse(answer,out number))l.answers=new[]{answer,(number+1).ToString(),(number-1).ToString()};
        l.correct=(topic+tier)%3;string first=l.answers[0];l.answers[0]=l.answers[l.correct];l.answers[l.correct]=first;
        SetDemonstration(l,lang,topic,hard);
        return l;
    }

    // Each tier owns its code and trace. The independent prediction above keeps
    // its own code and values, separate from the demonstration.
    private static void SetDemonstration(VisualCodeLessons.Lesson l,string lang,int topic,bool hard)
    {
        bool py=lang=="python",cs=lang=="csharp";
        Func<string,string> print=x=>py?"print("+x+")":(cs?"Console.WriteLine(":"System.out.println(")+x+");";
        Func<string,string,string> assign=(name,value)=>py?name+" = "+value:"int "+name+" = "+value+";";
        var steps=new System.Collections.Generic.List<VisualCodeLessons.Step>();
        Action<int,string,string,int,string[]> add=(line,why,output,active,cells)=>steps.Add(new VisualCodeLessons.Step(line,why,output,active,cells));
        switch(topic)
        {
            case 0:
                l.code=hard?print("(3 + 1) * 2")+"\n"+print("\"Lantern ready\""):print("3 + 4");
                add(-1,"Nothing has been printed. Step through this level's example.","",-1,new[]{"Messages\nempty"});
                add(0,hard?"Add inside the parentheses first: 3 + 1 is 4. Multiply 4 by 2, then show 8.":"These numbers have no quotes. Add 3 and 4, then show the answer, 7.",hard?"8":"7",0,new[]{"Messages\n1 line"});
                if(hard)add(1,"The next print instruction adds Lantern ready on a new line. The earlier 8 stays visible.","8\nLantern ready",0,new[]{"Messages\n2 lines"});
                break;
            case 1:
                l.code=assign("coins","3")+"\ncoins = coins * 2"+(py?"":";")+"\n"+(hard?"coins = coins - 1"+(py?"":";")+"\n":"")+print("coins");
                add(-1,"The coins box is empty. Follow how each assignment changes it.","",-1,new[]{"coins\nnot set"});
                add(0,"Store 3 in the box named coins.","",0,new[]{"coins\n3"});
                add(1,"Read 3, multiply by 2, and store 6 back in coins.","",0,new[]{"coins\n6"});
                if(hard)add(2,"Use the latest value, 6. Subtract 1 and store 5.","",0,new[]{"coins\n5"});
                add(hard?3:2,"Print the stored value. Changing this example's coins does not change your hero's gold.",hard?"5":"6",0,new[]{"coins\n"+(hard?"5":"6")});
                break;
            case 2:
                string test=hard?(py?"keys >= 1 and coins >= 5":"keys >= 1 && coins >= 5"):"coins >= 5";
                l.code=(hard?assign("keys","0")+"\n":"")+assign("coins","7")+"\n"+(py?"if "+test+":\n    "+print("\"Open\"")+"\nelse:\n    "+print("\"Locked\""):"if ("+test+") {\n    "+print("\"Open\"")+"\n} else {\n    "+print("\"Locked\"")+"\n}");
                add(-1,"The gate is waiting. Read the stored values before choosing a path.","",-1,hard?new[]{"keys\nnot set","coins\nnot set","Gate\nwaiting"}:new[]{"coins\nnot set","Gate\nwaiting"});
                if(hard)add(0,"Store 0 keys. Having enough coins will not replace the missing key.","",0,new[]{"keys\n0","coins\nnot set","Gate\nwaiting"});
                add(hard?1:0,"Store 7 coins.","",hard?1:0,hard?new[]{"keys\n0","coins\n7","Gate\nwaiting"}:new[]{"coins\n7","Gate\nwaiting"});
                add(hard?2:1,hard?"0 >= 1 is false. AND needs both tests to be true, so the combined test is false. The coin test is skipped.":"7 >= 5 is true. Use the if path and skip else.","",hard?2:1,hard?new[]{"keys\n0","coins\n7","Test\nFALSE"}:new[]{"coins\n7","Test\nTRUE"});
                add(hard?5:2,hard?"The test was false. Run only the else print instruction: Locked.":"The test was true. Run only the if print instruction: Open.",hard?"Locked":"Open",hard?2:1,hard?new[]{"keys\n0","coins\n7","Gate\nLOCKED"}:new[]{"coins\n7","Gate\nOPEN"});
                break;
            case 3:
                l.code=assign("total","0")+"\n"+(py?"for i in range(1, 3):\n    total = total + "+(hard?"i":"3"):"for (int i = 1; i < 3; i++) {\n    total = total + "+(hard?"i":"3")+";\n}")+"\n"+print("total");
                add(-1,"This loop will use counter values 1 and 2. Nothing is shown before the final print.","",-1,new[]{"i\nnot set","total\nnot set"});
                add(0,"Set the running total to 0.","",1,new[]{"i\nnot set","total\n0"});
                add(1,"The first pass starts with i = 1.","",0,new[]{"i\n1","total\n0"});
                add(2,hard?"Add the current counter, 1: total becomes 0 + 1 = 1.":"Add 3 to the latest total: 0 + 3 = 3.","",1,new[]{"i\n1","total\n"+(hard?"1":"3")});
                add(1,"The second pass uses i = 2.","",0,new[]{"i\n2","total\n"+(hard?"1":"3")});
                add(2,hard?"Add the current counter, 2: total becomes 1 + 2 = 3.":"Add 3 again: total becomes 3 + 3 = 6.","",1,new[]{"i\n2","total\n"+(hard?"3":"6")});
                add(1,"The next counter value would be 3. Stop before 3; the loop body does not run again.","",-1,new[]{"i\nstopped","total\n"+(hard?"3":"6")});
                add(py?3:4,"The print is outside the loop, so show the final total once.",hard?"3":"6",1,new[]{"i\nstopped","total\n"+(hard?"3":"6")});
                break;
            case 4:
                l.code=(py?"potions = [2, 5, 9]":"int[] potions = {2, 5, 9};")+"\n"+(hard?"potions[1] = potions[0] + 1"+(py?"":";")+"\n":"")+print(hard?"potions[1]":"potions[0] + potions[2]");
                add(-1,"The shelf is empty. Its three places are numbered 0, 1 and 2.","",-1,new[]{"index 0\nempty","index 1\nempty","index 2\nempty"});
                add(0,"Store 2, 5 and 9 in order. Index 0 holds 2; index 2 holds 9.","",-1,new[]{"index 0\n2","index 1\n5","index 2\n9"});
                if(hard)add(1,"Read 2 from index 0 and add 1. Replace only index 1 with 3.","",1,new[]{"index 0\n2","index 1\n3","index 2\n9"});
                add(hard?2:1,hard?"Read the updated value at index 1 and print 3.":"Read indexes 0 and 2. Add their values, 2 + 9, and print 11.",hard?"3":"11",hard?1:-1,hard?new[]{"index 0\n2","index 1\n3","index 2\n9"}:new[]{"index 0\n2","index 1\n5","index 2\n9"});
                break;
            default:
                string name=cs?"Heal":"heal";
                string calls=hard?name+"("+name+"(2))":name+"(2) + "+name+"(5)";
                l.code=(py?"def heal(hp):\n    return hp + 2":"static int "+name+"(int hp) {\n    return hp + 2;\n}")+"\n"+print(calls);
                int callLine=py?2:3;
                add(-1,"The definition is ready. Its body runs only when the final line calls it.","",-1,new[]{"input\nwaiting","hp\nnot set","result\nwaiting"});
                add(callLine,hard?"Start with the inside call. Its input is 2.":"Evaluate the first call with input 2.","",0,new[]{"input\n2","hp\n2","result\nwaiting"});
                add(1,"Return 2 + 2 = 4 to the calling line.","",2,new[]{"input\n2","hp\n2","result\n4"});
                add(callLine,hard?"Use that returned 4 as the outside call's input.":"Keep the first result, 4. The second call receives its own input, 5.","",0,new[]{"input\n"+(hard?"4":"5"),"hp\n"+(hard?"4":"5"),"first result\n4"});
                add(1,hard?"The outside call returns 4 + 2 = 6.":"The second call returns 5 + 2 = 7.","",2,new[]{"first result\n4","hp\n"+(hard?"4":"5"),"result\n"+(hard?"6":"7")});
                add(callLine,hard?"Print the result of the outside call: 6.":"Add the two returned results, 4 + 7. Print 11.",hard?"6":"11",2,new[]{"first result\n4","second result\n"+(hard?"6":"7"),"output\n"+(hard?"6":"11")});
                break;
        }
        l.steps=steps.ToArray();
    }
}

