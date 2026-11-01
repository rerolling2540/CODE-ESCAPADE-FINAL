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
        l.meaning+=(hard?" Challenge combines multiple operations. Solve the new example after reviewing the foundation trace.":" Applied practice uses the concept in a new situation. Review the foundation trace, then predict the new code.");
        string[] concepts={hard?" In this exercise, * means multiply. Work out the sum inside ( ) first, then multiply. Example: (1 + 2) * 3 becomes 3 * 3. Read the print lines from top to bottom.":" In this exercise, + adds numbers. Printing 1 + 2 shows 3. Quotes show words; without quotes, the computer can calculate.",hard?" This time, * means multiply and - means subtract. Update the box one line at a time. Always use the latest number in it.":" This time, * means multiply. Multiply the number in the box, then put the answer back into the same box.",hard?(py?" This time, and joins two tests. Both must say yes to open the gate. If even one says no, use else.":" This time, && means AND. Both tests must say yes to open the gate. If even one says no, use else."):" This time, check whether there are at least 5 coins. >= includes exactly 5. Choose the yes or no path; do not run both.",hard?" This time, total is a box that starts at 0. Add the current counter number on each repeat. Keep the updated total for the next repeat. Print it only after all repeats finish.":" This time, total is a box that starts at 0. Each repeat adds 2 to its latest value. After the loop ends, print the final total.",hard?" This time, = changes one shelf space. Read the value on the right, do the addition, then store the answer at the position on the left.":" This time, read two shelf spaces and add their stored numbers. Start counting positions at 0, not 1.",hard?" This time, one healing instruction is inside another. Work out the inside ( ) first. Its answer becomes the input for the outside instruction.":" This time, use the healing instructions twice, once for each input. Each use gives its own answer. Add those two answers."};
        l.meaning=VisualCodeLessons.Get(lang,topic).meaning+"\n\n"+concepts[topic].Trim();
        int number;
        if(int.TryParse(answer,out number))l.answers=new[]{answer,(number+1).ToString(),(number-1).ToString()};
        l.correct=(topic+tier)%3;string first=l.answers[0];l.answers[0]=l.answers[l.correct];l.answers[l.correct]=first;
        // Keep the introductory visual trace available as a scaffold; the new exercise is solved independently.
        return l;
    }
}

