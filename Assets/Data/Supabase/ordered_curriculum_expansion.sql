-- Original curriculum expansion. No deletes; existing question IDs/history preserved.
begin;
alter table public.question_bank add column if not exists curriculum_order integer;
create index if not exists question_bank_curriculum_idx on public.question_bank(language,curriculum_order,min_level) where published;
update public.question_bank set topic='output', difficulty='easy', min_level=1, max_level=null, curriculum_order=1 where id='csharp-e01' and prompt='What does this print?' and code_snippet='Console.WriteLine(2 + 3);' and published=true;
update public.question_bank set topic='variables', difficulty='easy', min_level=2, max_level=null, curriculum_order=4 where id='csharp-e02' and prompt='Fill the blank with the whole-number type.' and code_snippet='___ score = 12;' and published=true;
update public.question_bank set topic='strings', difficulty='easy', min_level=3, max_level=null, curriculum_order=6 where id='csharp-e03' and prompt='What is printed?' and code_snippet='string name = "Ada";\nConsole.WriteLine("Hi " + name);' and published=true;
update public.question_bank set topic='conditionals', difficulty='easy', min_level=4, max_level=null, curriculum_order=8 where id='csharp-e04' and prompt='Which operator checks whether age equals 18?' and code_snippet='if (age ___ 18) { ... }' and published=true;
update public.question_bank set topic='arrays', difficulty='easy', min_level=6, max_level=null, curriculum_order=10 where id='csharp-e05' and prompt='What is printed?' and code_snippet='int[] nums = { 4, 8 };\nConsole.WriteLine(nums[0]);' and published=true;
update public.question_bank set topic='methods', difficulty='easy', min_level=8, max_level=null, curriculum_order=11 where id='csharp-e06' and prompt='Choose the keyword that sends this result back.' and code_snippet='static int DoubleIt(int n) { ___ n * 2; }' and published=true;
update public.question_bank set topic='loops', difficulty='easy', min_level=5, max_level=null, curriculum_order=9 where id='csharp-e07' and prompt='What characters are printed?' and code_snippet='for (int i = 0; i < 3; i++) Console.Write(i);' and published=true;
update public.question_bank set topic='syntax', difficulty='easy', min_level=1, max_level=null, curriculum_order=2 where id='csharp-e08' and prompt='Choose the correction for this missing semicolon.' and code_snippet='int lives = 3' and published=true;
update public.question_bank set topic='comparisons', difficulty='easy', min_level=3, max_level=null, curriculum_order=7 where id='csharp-e09' and prompt='What is printed?' and code_snippet='Console.WriteLine(7 > 4);' and published=true;
update public.question_bank set topic='types', difficulty='easy', min_level=1, max_level=null, curriculum_order=3 where id='csharp-e10' and prompt='Which type stores true or false?' and code_snippet='' and published=true;
update public.question_bank set topic='recursion', difficulty='hard', min_level=18, max_level=null, curriculum_order=16 where id='csharp-h01' and prompt='What value is returned by this recursion?' and code_snippet='int F(int n) => n <= 1 ? 1 : n * F(n - 1);\nConsole.WriteLine(F(4));' and published=true;
update public.question_bank set topic='algorithms', difficulty='hard', min_level=16, max_level=null, curriculum_order=15 where id='csharp-h02' and prompt='What is binary search time complexity on a sorted array?' and code_snippet='' and published=true;
update public.question_bank set topic='collections', difficulty='hard', min_level=16, max_level=null, curriculum_order=13 where id='csharp-h03' and prompt='What is printed?' and code_snippet='var xs = new List<int> { 1, 2 };\nxs.Add(3);\nConsole.WriteLine(xs.Count);' and published=true;
update public.question_bank set topic='objects', difficulty='hard', min_level=16, max_level=null, curriculum_order=12 where id='csharp-h04' and prompt='How should a custom class compare object values?' and code_snippet='if (a == b) { /* equal contents? */ }' and published=true;
update public.question_bank set topic='exceptions', difficulty='hard', min_level=16, max_level=null, curriculum_order=14 where id='csharp-h05' and prompt='What is printed?' and code_snippet='try { int x = 10 / int.Parse("0"); } catch (DivideByZeroException) { Console.WriteLine("caught"); }' and published=true;
update public.question_bank set topic='collections', difficulty='hard', min_level=16, max_level=null, curriculum_order=13 where id='csharp-h06' and prompt='Fill in the generic type for a list of strings.' and code_snippet='List<___> names = new List<string>();' and published=true;
update public.question_bank set topic='operators', difficulty='easy', min_level=2, max_level=null, curriculum_order=5 where id='csharp-h07' and prompt='What is printed?' and code_snippet='int x = 2;\nConsole.WriteLine(x * (x + 3));' and published=true;
update public.question_bank set topic='collections', difficulty='hard', min_level=16, max_level=null, curriculum_order=13 where id='csharp-h08' and prompt='Which structure is last-in, first-out?' and code_snippet='' and published=true;
update public.question_bank set topic='loops', difficulty='hard', min_level=16, max_level=null, curriculum_order=9 where id='csharp-h09' and prompt='What is printed?' and code_snippet='int[] a = { 2, 4, 6 };\nint total = 0;\nforeach (int v in a) total += v / 2;\nConsole.WriteLine(total);' and published=true;
update public.question_bank set topic='recursion', difficulty='hard', min_level=18, max_level=null, curriculum_order=16 where id='csharp-h10' and prompt='What prevents recursion from continuing forever?' and code_snippet='' and published=true;
update public.question_bank set topic='loops', difficulty='medium', min_level=6, max_level=null, curriculum_order=9 where id='csharp-m01' and prompt='What is the final value of sum?' and code_snippet='int sum = 0;\nforeach (int n in new[] { 2, 4, 6 }) sum += n;\nConsole.WriteLine(sum);' and published=true;
update public.question_bank set topic='strings', difficulty='medium', min_level=6, max_level=null, curriculum_order=6 where id='csharp-m02' and prompt='What does this print?' and code_snippet='string word = "code";\nConsole.WriteLine(word.Substring(1, 2));' and published=true;
update public.question_bank set topic='arrays', difficulty='medium', min_level=6, max_level=null, curriculum_order=10 where id='csharp-m03' and prompt='Fill the condition to visit indexes safely.' and code_snippet='for (int i = 0; i ___ values.Length; i++)' and published=true;
update public.question_bank set topic='operators', difficulty='medium', min_level=6, max_level=null, curriculum_order=5 where id='csharp-m04' and prompt='What is printed?' and code_snippet='int x = 5;\nConsole.WriteLine(x++);\nConsole.WriteLine(x);' and published=true;
update public.question_bank set topic='strings', difficulty='medium', min_level=6, max_level=null, curriculum_order=6 where id='csharp-m05' and prompt='Which comparison checks string contents?' and code_snippet='if (text == "yes") { ... }' and published=true;
update public.question_bank set topic='arrays', difficulty='medium', min_level=6, max_level=null, curriculum_order=10 where id='csharp-m06' and prompt='What is printed?' and code_snippet='int[] a = { 3, 1, 4 };\nConsole.WriteLine(a.Length);' and published=true;
update public.question_bank set topic='methods', difficulty='medium', min_level=8, max_level=null, curriculum_order=11 where id='csharp-m07' and prompt='Fill in the return type for this method.' and code_snippet='___ Add(int a, int b) { return a + b; }' and published=true;
update public.question_bank set topic='conditionals', difficulty='medium', min_level=6, max_level=null, curriculum_order=8 where id='csharp-m08' and prompt='What is printed?' and code_snippet='int n = 9;\nConsole.WriteLine(n % 2 == 0 ? "even" : "odd");' and published=true;
update public.question_bank set topic='loops', difficulty='medium', min_level=6, max_level=null, curriculum_order=9 where id='csharp-m09' and prompt='Which keyword skips to the next loop iteration?' and code_snippet='if (i == 2) ___;' and published=true;
update public.question_bank set topic='arrays', difficulty='medium', min_level=6, max_level=null, curriculum_order=10 where id='csharp-m10' and prompt='What is printed?' and code_snippet='int[] a = { 1, 2 };\na[1] = a[0] + 5;\nConsole.WriteLine(a[1]);' and published=true;
update public.question_bank set topic='output', difficulty='easy', min_level=1, max_level=null, curriculum_order=1 where id='java-e01' and prompt='What does this print?' and code_snippet='System.out.println(2 + 3);' and published=true;
update public.question_bank set topic='variables', difficulty='easy', min_level=2, max_level=null, curriculum_order=4 where id='java-e02' and prompt='Fill the blank with the whole-number type.' and code_snippet='___ score = 12;' and published=true;
update public.question_bank set topic='strings', difficulty='easy', min_level=3, max_level=null, curriculum_order=6 where id='java-e03' and prompt='What is printed?' and code_snippet='String name = "Ada";\nSystem.out.println("Hi " + name);' and published=true;
update public.question_bank set topic='conditionals', difficulty='easy', min_level=4, max_level=null, curriculum_order=8 where id='java-e04' and prompt='Which operator checks whether age equals 18?' and code_snippet='if (age ___ 18) { ... }' and published=true;
update public.question_bank set topic='arrays', difficulty='easy', min_level=6, max_level=null, curriculum_order=10 where id='java-e05' and prompt='What is printed?' and code_snippet='int[] nums = {4, 8};\nSystem.out.println(nums[0]);' and published=true;
update public.question_bank set topic='methods', difficulty='easy', min_level=8, max_level=null, curriculum_order=11 where id='java-e06' and prompt='Choose the keyword that makes this method return a value.' and code_snippet='static int doubleIt(int n) { ___ n * 2; }' and published=true;
update public.question_bank set topic='loops', difficulty='easy', min_level=5, max_level=null, curriculum_order=9 where id='java-e07' and prompt='What characters are printed?' and code_snippet='for (int i = 0; i < 3; i++) System.out.print(i);' and published=true;
update public.question_bank set topic='syntax', difficulty='easy', min_level=1, max_level=null, curriculum_order=2 where id='java-e08' and prompt='Choose the correction for this missing semicolon.' and code_snippet='int lives = 3' and published=true;
update public.question_bank set topic='comparisons', difficulty='easy', min_level=3, max_level=null, curriculum_order=7 where id='java-e09' and prompt='What is printed?' and code_snippet='System.out.println(7 > 4);' and published=true;
update public.question_bank set topic='types', difficulty='easy', min_level=1, max_level=null, curriculum_order=3 where id='java-e10' and prompt='Which type stores true or false?' and code_snippet='' and published=true;
update public.question_bank set topic='recursion', difficulty='hard', min_level=18, max_level=null, curriculum_order=16 where id='java-h01' and prompt='What value is returned by this recursion?' and code_snippet='static int f(int n) { return n <= 1 ? 1 : n * f(n - 1); }\nSystem.out.println(f(4));' and published=true;
update public.question_bank set topic='algorithms', difficulty='hard', min_level=16, max_level=null, curriculum_order=15 where id='java-h02' and prompt='What is the time complexity of binary search on a sorted array?' and code_snippet='' and published=true;
update public.question_bank set topic='collections', difficulty='hard', min_level=16, max_level=null, curriculum_order=13 where id='java-h03' and prompt='What is printed?' and code_snippet='List<Integer> xs = new ArrayList<>(List.of(1, 2));\nxs.add(3);\nSystem.out.println(xs.size());' and published=true;
update public.question_bank set topic='objects', difficulty='hard', min_level=16, max_level=null, curriculum_order=12 where id='java-h04' and prompt='How should this code compare two objects by value?' and code_snippet='if (a == b) { /* equal contents? */ }' and published=true;
update public.question_bank set topic='exceptions', difficulty='hard', min_level=16, max_level=null, curriculum_order=14 where id='java-h05' and prompt='What is printed?' and code_snippet='try { int x = 10 / 0; } catch (ArithmeticException e) { System.out.println("caught"); }' and published=true;
update public.question_bank set topic='collections', difficulty='hard', min_level=16, max_level=null, curriculum_order=13 where id='java-h06' and prompt='Fill in the generic type parameter for a list of strings.' and code_snippet='List<___> names = new ArrayList<>();' and published=true;
update public.question_bank set topic='operators', difficulty='easy', min_level=2, max_level=null, curriculum_order=5 where id='java-h07' and prompt='What is printed?' and code_snippet='int x = 2;\nSystem.out.println(x * (x + 3));' and published=true;
update public.question_bank set topic='collections', difficulty='hard', min_level=16, max_level=null, curriculum_order=13 where id='java-h08' and prompt='Which data structure is designed for last-in, first-out access?' and code_snippet='' and published=true;
update public.question_bank set topic='loops', difficulty='hard', min_level=16, max_level=null, curriculum_order=9 where id='java-h09' and prompt='What is printed?' and code_snippet='int[] a = {2, 4, 6};\nint total = 0;\nfor (int v : a) total += v / 2;\nSystem.out.println(total);' and published=true;
update public.question_bank set topic='recursion', difficulty='hard', min_level=18, max_level=null, curriculum_order=16 where id='java-h10' and prompt='What must a recursive method have to eventually stop calling itself?' and code_snippet='' and published=true;
update public.question_bank set topic='loops', difficulty='medium', min_level=6, max_level=null, curriculum_order=9 where id='java-m01' and prompt='What is the final value of sum?' and code_snippet='int sum = 0;\nfor (int n : new int[]{2, 4, 6}) sum += n;\nSystem.out.println(sum);' and published=true;
update public.question_bank set topic='strings', difficulty='medium', min_level=6, max_level=null, curriculum_order=6 where id='java-m02' and prompt='What does this print?' and code_snippet='String word = "code";\nSystem.out.println(word.substring(1, 3));' and published=true;
update public.question_bank set topic='arrays', difficulty='medium', min_level=6, max_level=null, curriculum_order=10 where id='java-m03' and prompt='Fill the condition to visit indexes 0 through 2 safely.' and code_snippet='for (int i = 0; i ___ values.length; i++)' and published=true;
update public.question_bank set topic='operators', difficulty='medium', min_level=6, max_level=null, curriculum_order=5 where id='java-m04' and prompt='What is printed?' and code_snippet='int x = 5;\nSystem.out.println(x++);\nSystem.out.println(x);' and published=true;
update public.question_bank set topic='strings', difficulty='medium', min_level=6, max_level=null, curriculum_order=6 where id='java-m05' and prompt='Which line safely checks whether text is the word "yes"?' and code_snippet='if (text == "yes") { ... }' and published=true;
update public.question_bank set topic='arrays', difficulty='medium', min_level=6, max_level=null, curriculum_order=10 where id='java-m06' and prompt='What is printed?' and code_snippet='int[] a = {3, 1, 4};\nSystem.out.println(a.length);' and published=true;
update public.question_bank set topic='methods', difficulty='medium', min_level=8, max_level=null, curriculum_order=11 where id='java-m07' and prompt='Fill in the return type for this method.' and code_snippet='___ add(int a, int b) { return a + b; }' and published=true;
update public.question_bank set topic='conditionals', difficulty='medium', min_level=6, max_level=null, curriculum_order=8 where id='java-m08' and prompt='What is printed?' and code_snippet='int n = 9;\nif (n % 2 == 0) System.out.println("even"); else System.out.println("odd");' and published=true;
update public.question_bank set topic='loops', difficulty='medium', min_level=6, max_level=null, curriculum_order=9 where id='java-m09' and prompt='Which keyword skips to the next loop iteration?' and code_snippet='if (i == 2) ___;' and published=true;
update public.question_bank set topic='arrays', difficulty='medium', min_level=6, max_level=null, curriculum_order=10 where id='java-m10' and prompt='What is printed?' and code_snippet='int[] a = {1, 2};\na[1] = a[0] + 5;\nSystem.out.println(a[1]);' and published=true;
update public.question_bank set topic='output', difficulty='easy', min_level=1, max_level=null, curriculum_order=1 where id='python-e01' and prompt='What does this print?' and code_snippet='print(2 + 3)' and published=true;
update public.question_bank set topic='variables', difficulty='easy', min_level=2, max_level=null, curriculum_order=4 where id='python-e02' and prompt='Fill the blank with a Python whole-number value.' and code_snippet='score = ___' and published=true;
update public.question_bank set topic='strings', difficulty='easy', min_level=3, max_level=null, curriculum_order=6 where id='python-e03' and prompt='What is printed?' and code_snippet='name = "Ada"\nprint("Hi " + name)' and published=true;
update public.question_bank set topic='conditionals', difficulty='easy', min_level=4, max_level=null, curriculum_order=8 where id='python-e04' and prompt='Which operator checks whether age equals 18?' and code_snippet='if age ___ 18:' and published=true;
update public.question_bank set topic='arrays', difficulty='easy', min_level=6, max_level=null, curriculum_order=10 where id='python-e05' and prompt='What is printed?' and code_snippet='nums = [4, 8]\nprint(nums[0])' and published=true;
update public.question_bank set topic='methods', difficulty='easy', min_level=8, max_level=null, curriculum_order=11 where id='python-e06' and prompt='Which keyword returns the result from this function?' and code_snippet='def double_it(n):\n    ___ n * 2' and published=true;
update public.question_bank set topic='loops', difficulty='easy', min_level=5, max_level=null, curriculum_order=9 where id='python-e07' and prompt='What numbers are printed?' and code_snippet='for i in range(3):\n    print(i, end="")' and published=true;
update public.question_bank set topic='syntax', difficulty='easy', min_level=1, max_level=null, curriculum_order=2 where id='python-e08' and prompt='Choose the correction for this assignment.' and code_snippet='lives == 3' and published=true;
update public.question_bank set topic='comparisons', difficulty='easy', min_level=3, max_level=null, curriculum_order=7 where id='python-e09' and prompt='What is printed?' and code_snippet='print(7 > 4)' and published=true;
update public.question_bank set topic='types', difficulty='easy', min_level=1, max_level=null, curriculum_order=3 where id='python-e10' and prompt='Which literal represents true in Python?' and code_snippet='' and published=true;
update public.question_bank set topic='recursion', difficulty='hard', min_level=18, max_level=null, curriculum_order=16 where id='python-h01' and prompt='What value does this recursion return?' and code_snippet='def f(n):\n    return 1 if n <= 1 else n * f(n - 1)\nprint(f(4))' and published=true;
update public.question_bank set topic='algorithms', difficulty='hard', min_level=16, max_level=null, curriculum_order=15 where id='python-h02' and prompt='What is binary search time complexity on a sorted list?' and code_snippet='' and published=true;
update public.question_bank set topic='collections', difficulty='hard', min_level=16, max_level=null, curriculum_order=13 where id='python-h03' and prompt='What is printed?' and code_snippet='xs = [1, 2]\nxs.append(3)\nprint(len(xs))' and published=true;
update public.question_bank set topic='objects', difficulty='hard', min_level=16, max_level=null, curriculum_order=12 where id='python-h04' and prompt='Which method defines value equality for a custom class?' and code_snippet='class Token: pass' and published=true;
update public.question_bank set topic='exceptions', difficulty='hard', min_level=16, max_level=null, curriculum_order=14 where id='python-h05' and prompt='What is printed?' and code_snippet='try:\n    x = 10 // 0\nexcept ZeroDivisionError:\n    print("caught")' and published=true;
update public.question_bank set topic='collections', difficulty='hard', min_level=16, max_level=null, curriculum_order=13 where id='python-h06' and prompt='Fill in the expression to keep only even numbers.' and code_snippet='evens = [n for n in nums if n ___ 2 == 0]' and published=true;
update public.question_bank set topic='operators', difficulty='easy', min_level=2, max_level=null, curriculum_order=5 where id='python-h07' and prompt='What is printed?' and code_snippet='x = 2\nprint(x * (x + 3))' and published=true;
update public.question_bank set topic='collections', difficulty='hard', min_level=16, max_level=null, curriculum_order=13 where id='python-h08' and prompt='Which standard collection is last-in, first-out when used with append and pop?' and code_snippet='' and published=true;
update public.question_bank set topic='loops', difficulty='hard', min_level=16, max_level=null, curriculum_order=9 where id='python-h09' and prompt='What is printed?' and code_snippet='a = [2, 4, 6]\ntotal = sum(v // 2 for v in a)\nprint(total)' and published=true;
update public.question_bank set topic='recursion', difficulty='hard', min_level=18, max_level=null, curriculum_order=16 where id='python-h10' and prompt='What prevents recursion from continuing forever?' and code_snippet='' and published=true;
update public.question_bank set topic='loops', difficulty='medium', min_level=6, max_level=null, curriculum_order=9 where id='python-m01' and prompt='What is the final value of total?' and code_snippet='total = 0\nfor n in [2, 4, 6]:\n    total += n\nprint(total)' and published=true;
update public.question_bank set topic='strings', difficulty='medium', min_level=6, max_level=null, curriculum_order=6 where id='python-m02' and prompt='What does this print?' and code_snippet='word = "code"\nprint(word[1:3])' and published=true;
update public.question_bank set topic='arrays', difficulty='medium', min_level=6, max_level=null, curriculum_order=10 where id='python-m03' and prompt='Fill the range bound to visit every valid index.' and code_snippet='for i in range(___):\n    print(values[i])' and published=true;
update public.question_bank set topic='operators', difficulty='medium', min_level=6, max_level=null, curriculum_order=5 where id='python-m04' and prompt='What is printed?' and code_snippet='x = 5\nprint(x)\nx += 1\nprint(x)' and published=true;
update public.question_bank set topic='strings', difficulty='medium', min_level=6, max_level=null, curriculum_order=6 where id='python-m05' and prompt='Choose the equality check for the word yes.' and code_snippet='if text = "yes":' and published=true;
update public.question_bank set topic='arrays', difficulty='medium', min_level=6, max_level=null, curriculum_order=10 where id='python-m06' and prompt='What is printed?' and code_snippet='a = [3, 1, 4]\nprint(len(a))' and published=true;
update public.question_bank set topic='methods', difficulty='medium', min_level=8, max_level=null, curriculum_order=11 where id='python-m07' and prompt='Fill in the function keyword.' and code_snippet='___ add(a, b):\n    return a + b' and published=true;
update public.question_bank set topic='conditionals', difficulty='medium', min_level=6, max_level=null, curriculum_order=8 where id='python-m08' and prompt='What is printed?' and code_snippet='n = 9\nprint("even" if n % 2 == 0 else "odd")' and published=true;
update public.question_bank set topic='loops', difficulty='medium', min_level=6, max_level=null, curriculum_order=9 where id='python-m09' and prompt='Which keyword skips to the next loop iteration?' and code_snippet='if i == 2:\n    ___' and published=true;
update public.question_bank set topic='arrays', difficulty='medium', min_level=6, max_level=null, curriculum_order=10 where id='python-m10' and prompt='What is printed?' and code_snippet='a = [1, 2]\na[1] = a[0] + 5\nprint(a[1])' and published=true;
insert into public.question_bank (id,language,topic,difficulty,min_level,max_level,question_type,prompt,code_snippet,choices,correct_index,explanation,published,curriculum_order) values
('curriculum-v2-java-output-easy-01','java','output','easy',1,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println("Lantern ready");','["Lantern ready", "No output", "An error occurs", "Lantern ready!"]'::jsonb,0,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-java-output-easy-02','java','output','easy',1,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println("Bridge repaired");','["No output", "Bridge repaired", "An error occurs", "Bridge repaired!"]'::jsonb,1,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-java-output-easy-03','java','output','easy',1,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println("Quest accepted");','["An error occurs", "No output", "Quest accepted", "Quest accepted!"]'::jsonb,2,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-java-output-easy-04','java','output','easy',1,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println("Shield raised");','["Shield raised!", "No output", "An error occurs", "Shield raised"]'::jsonb,3,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-java-syntax-easy-01','java','syntax','easy',1,null,'fill_blank','Fill the blank with the punctuation that makes this code valid.','int coins = 3___','[":", ";", "==", ","]'::jsonb,1,'Python starts an indented block with a colon. Java/C# end this assignment statement with a semicolon.',true,2),
('curriculum-v2-java-syntax-easy-02','java','syntax','easy',1,null,'fill_blank','Fill the blank with the punctuation that makes this code valid.','int coins = 4___','["==", ":", ";", ","]'::jsonb,2,'Python starts an indented block with a colon. Java/C# end this assignment statement with a semicolon.',true,2),
('curriculum-v2-java-syntax-easy-03','java','syntax','easy',1,null,'fill_blank','Fill the blank with the punctuation that makes this code valid.','int coins = 5___','[",", ":", "==", ";"]'::jsonb,3,'Python starts an indented block with a colon. Java/C# end this assignment statement with a semicolon.',true,2),
('curriculum-v2-java-syntax-easy-04','java','syntax','easy',1,null,'fill_blank','Fill the blank with the punctuation that makes this code valid.','int coins = 6___','[";", ":", "==", ","]'::jsonb,0,'Python starts an indented block with a colon. Java/C# end this assignment statement with a semicolon.',true,2),
('curriculum-v2-java-types-easy-01','java','types','easy',1,null,'multiple_choice','Which type name describes a whole number, such as 3?','','["boolean", "String", "int", "double"]'::jsonb,2,'int is the type name for whole numbers, such as 3 or 12.',true,3),
('curriculum-v2-java-types-easy-02','java','types','easy',1,null,'multiple_choice','Which type name describes a whole number, such as 4?','','["double", "String", "boolean", "int"]'::jsonb,3,'int is the type name for whole numbers, such as 3 or 12.',true,3),
('curriculum-v2-java-types-easy-03','java','types','easy',1,null,'multiple_choice','Which type name describes a whole number, such as 5?','','["int", "String", "boolean", "double"]'::jsonb,0,'int is the type name for whole numbers, such as 3 or 12.',true,3),
('curriculum-v2-java-types-easy-04','java','types','easy',1,null,'multiple_choice','Which type name describes a whole number, such as 6?','','["String", "int", "boolean", "double"]'::jsonb,1,'int is the type name for whole numbers, such as 3 or 12.',true,3),
('curriculum-v2-java-variables-easy-01','java','variables','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 3;
System.out.println(coins);','["6", "4", "2", "3"]'::jsonb,3,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-java-variables-easy-02','java','variables','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 4;
System.out.println(coins);','["4", "5", "3", "7"]'::jsonb,0,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-java-variables-easy-03','java','variables','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 5;
System.out.println(coins);','["6", "5", "4", "8"]'::jsonb,1,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-java-variables-easy-04','java','variables','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 6;
System.out.println(coins);','["5", "7", "6", "9"]'::jsonb,2,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-java-operators-easy-01','java','operators','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(3 + 2);','["5", "6", "4", "8"]'::jsonb,0,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-java-operators-easy-02','java','operators','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(4 + 3);','["8", "7", "6", "10"]'::jsonb,1,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-java-operators-easy-03','java','operators','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(5 + 4);','["8", "10", "9", "12"]'::jsonb,2,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-java-operators-easy-04','java','operators','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(6 + 5);','["14", "12", "10", "11"]'::jsonb,3,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-java-strings-easy-01','java','strings','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println("Hi " + "Lantern");','["No output", "Hi Lantern", "An error occurs", "Hi Lantern!"]'::jsonb,1,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-java-strings-easy-02','java','strings','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println("Hi " + "Warrior");','["An error occurs", "No output", "Hi Warrior", "Hi Warrior!"]'::jsonb,2,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-java-strings-easy-03','java','strings','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println("Hi " + "Journey");','["Hi Journey!", "No output", "An error occurs", "Hi Journey"]'::jsonb,3,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-java-strings-easy-04','java','strings','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println("Hi " + "Crystal");','["Hi Crystal", "No output", "An error occurs", "Hi Crystal!"]'::jsonb,0,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-java-comparisons-easy-01','java','comparisons','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(3 > 2);','["No output", "false", "true", "An error occurs"]'::jsonb,2,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-java-comparisons-easy-02','java','comparisons','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(3 >= 4);','["An error occurs", "true", "No output", "false"]'::jsonb,3,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-java-comparisons-easy-03','java','comparisons','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(5 > 4);','["true", "false", "No output", "An error occurs"]'::jsonb,0,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-java-comparisons-easy-04','java','comparisons','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(5 >= 6);','["true", "false", "No output", "An error occurs"]'::jsonb,1,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-java-conditionals-easy-01','java','conditionals','easy',4,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 3;
if (coins >= 5) {
    System.out.println("Open");
} else {
    System.out.println("Locked");
}','["Locked!", "No output", "An error occurs", "Locked"]'::jsonb,3,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-java-conditionals-easy-02','java','conditionals','easy',4,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 4;
if (coins >= 5) {
    System.out.println("Open");
} else {
    System.out.println("Locked");
}','["Locked", "No output", "An error occurs", "Locked!"]'::jsonb,0,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-java-conditionals-easy-03','java','conditionals','easy',4,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 5;
if (coins >= 5) {
    System.out.println("Open");
} else {
    System.out.println("Locked");
}','["No output", "Open", "An error occurs", "Open!"]'::jsonb,1,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-java-conditionals-easy-04','java','conditionals','easy',4,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 6;
if (coins >= 5) {
    System.out.println("Open");
} else {
    System.out.println("Locked");
}','["An error occurs", "No output", "Open", "Open!"]'::jsonb,2,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-java-loops-easy-01','java','loops','easy',5,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','for (int i = 0; i < 2; i++) {
    System.out.println(i);
}','["0\n1", "No output", "An error occurs", "0\n1!"]'::jsonb,0,'The counter starts at 0 and stops before 2. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-java-loops-easy-02','java','loops','easy',5,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','for (int i = 0; i < 3; i++) {
    System.out.println(i);
}','["No output", "0\n1\n2", "An error occurs", "0\n1\n2!"]'::jsonb,1,'The counter starts at 0 and stops before 3. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-java-loops-easy-03','java','loops','easy',5,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','for (int i = 0; i < 4; i++) {
    System.out.println(i);
}','["An error occurs", "No output", "0\n1\n2\n3", "0\n1\n2\n3!"]'::jsonb,2,'The counter starts at 0 and stops before 4. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-java-loops-easy-04','java','loops','easy',5,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','for (int i = 0; i < 5; i++) {
    System.out.println(i);
}','["0\n1\n2\n3\n4!", "No output", "An error occurs", "0\n1\n2\n3\n4"]'::jsonb,3,'The counter starts at 0 and stops before 5. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-java-arrays-easy-01','java','arrays','easy',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {3, 6, 8};
System.out.println(potions[0]);','["4", "3", "2", "6"]'::jsonb,1,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-java-arrays-easy-02','java','arrays','easy',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {4, 7, 9};
System.out.println(potions[1]);','["6", "8", "7", "10"]'::jsonb,2,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-java-arrays-easy-03','java','arrays','easy',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {5, 8, 10};
System.out.println(potions[2]);','["13", "11", "9", "10"]'::jsonb,3,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-java-arrays-easy-04','java','arrays','easy',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {6, 9, 11};
System.out.println(potions[0]);','["6", "7", "5", "9"]'::jsonb,0,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-java-methods-easy-01','java','methods','easy',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int boost(int hp) {
    return hp + 2;
}
System.out.println(boost(3));','["4", "6", "5", "8"]'::jsonb,2,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-java-methods-easy-02','java','methods','easy',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int boost(int hp) {
    return hp + 2;
}
System.out.println(boost(4));','["9", "7", "5", "6"]'::jsonb,3,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-java-methods-easy-03','java','methods','easy',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int boost(int hp) {
    return hp + 2;
}
System.out.println(boost(5));','["7", "8", "6", "10"]'::jsonb,0,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-java-methods-easy-04','java','methods','easy',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int boost(int hp) {
    return hp + 2;
}
System.out.println(boost(6));','["9", "8", "7", "11"]'::jsonb,1,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-java-output-medium-01','java','output','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(3 + 2);
System.out.println("Ready");','["No output", "5\nReady", "An error occurs", "5\nReady!"]'::jsonb,1,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-java-output-medium-02','java','output','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(4 + 3);
System.out.println("Ready");','["An error occurs", "No output", "7\nReady", "7\nReady!"]'::jsonb,2,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-java-output-medium-03','java','output','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(5 + 4);
System.out.println("Ready");','["9\nReady!", "No output", "An error occurs", "9\nReady"]'::jsonb,3,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-java-output-medium-04','java','output','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(6 + 5);
System.out.println("Ready");','["11\nReady", "No output", "An error occurs", "11\nReady!"]'::jsonb,0,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-java-variables-medium-01','java','variables','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 3;
coins = coins + 2;
System.out.println(coins);','["5", "6", "4", "8"]'::jsonb,0,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-java-variables-medium-02','java','variables','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 4;
coins = coins + 3;
System.out.println(coins);','["8", "7", "6", "10"]'::jsonb,1,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-java-variables-medium-03','java','variables','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 5;
coins = coins + 4;
System.out.println(coins);','["8", "10", "9", "12"]'::jsonb,2,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-java-variables-medium-04','java','variables','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 6;
coins = coins + 5;
System.out.println(coins);','["14", "12", "10", "11"]'::jsonb,3,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-java-operators-medium-01','java','operators','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(3 * 2 - 1);','["6", "5", "4", "8"]'::jsonb,1,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-java-operators-medium-02','java','operators','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(4 * 3 - 1);','["10", "12", "11", "14"]'::jsonb,2,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-java-operators-medium-03','java','operators','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(5 * 4 - 1);','["22", "20", "18", "19"]'::jsonb,3,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-java-operators-medium-04','java','operators','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(6 * 5 - 1);','["29", "30", "28", "32"]'::jsonb,0,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-java-strings-medium-01','java','strings','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println("Lantern".length());','["6", "8", "7", "10"]'::jsonb,2,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-java-strings-medium-02','java','strings','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println("Warrior".length());','["10", "8", "6", "7"]'::jsonb,3,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-java-strings-medium-03','java','strings','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println("Journey".length());','["7", "8", "6", "10"]'::jsonb,0,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-java-strings-medium-04','java','strings','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println("Crystal".length());','["8", "7", "6", "10"]'::jsonb,1,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-java-comparisons-medium-01','java','comparisons','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(3 > 2 && 3 < 10);','["An error occurs", "false", "No output", "true"]'::jsonb,3,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-java-comparisons-medium-02','java','comparisons','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(3 >= 4 && 4 < 10);','["false", "true", "No output", "An error occurs"]'::jsonb,0,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-java-comparisons-medium-03','java','comparisons','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(5 > 4 && 5 < 10);','["false", "true", "No output", "An error occurs"]'::jsonb,1,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-java-comparisons-medium-04','java','comparisons','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println(5 >= 6 && 6 < 10);','["No output", "true", "false", "An error occurs"]'::jsonb,2,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-java-conditionals-medium-01','java','conditionals','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 3;
int keys = 0;
if (coins >= 5 && keys >= 1) {
    System.out.println("Open");
} else {
    System.out.println("Locked");
}','["Locked", "No output", "An error occurs", "Locked!"]'::jsonb,0,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-java-conditionals-medium-02','java','conditionals','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 4;
int keys = 1;
if (coins >= 5 && keys >= 1) {
    System.out.println("Open");
} else {
    System.out.println("Locked");
}','["No output", "Locked", "An error occurs", "Locked!"]'::jsonb,1,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-java-conditionals-medium-03','java','conditionals','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 5;
int keys = 0;
if (coins >= 5 && keys >= 1) {
    System.out.println("Open");
} else {
    System.out.println("Locked");
}','["An error occurs", "No output", "Locked", "Locked!"]'::jsonb,2,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-java-conditionals-medium-04','java','conditionals','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 6;
int keys = 1;
if (coins >= 5 && keys >= 1) {
    System.out.println("Open");
} else {
    System.out.println("Locked");
}','["Open!", "No output", "An error occurs", "Open"]'::jsonb,3,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-java-loops-medium-01','java','loops','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int total = 0;
for (int i = 0; i < 2; i++) {
    total = total + 2;
}
System.out.println(total);','["5", "4", "3", "7"]'::jsonb,1,'The counter starts at 0 and stops before 2. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-java-loops-medium-02','java','loops','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int total = 0;
for (int i = 0; i < 3; i++) {
    total = total + 2;
}
System.out.println(total);','["5", "7", "6", "9"]'::jsonb,2,'The counter starts at 0 and stops before 3. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-java-loops-medium-03','java','loops','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int total = 0;
for (int i = 0; i < 4; i++) {
    total = total + 2;
}
System.out.println(total);','["11", "9", "7", "8"]'::jsonb,3,'The counter starts at 0 and stops before 4. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-java-loops-medium-04','java','loops','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int total = 0;
for (int i = 0; i < 5; i++) {
    total = total + 2;
}
System.out.println(total);','["10", "11", "9", "13"]'::jsonb,0,'The counter starts at 0 and stops before 5. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-java-arrays-medium-01','java','arrays','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {3, 6, 8};
potions[1] = potions[0] + 2;
System.out.println(potions[1]);','["4", "6", "5", "8"]'::jsonb,2,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-java-arrays-medium-02','java','arrays','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {4, 7, 9};
potions[1] = potions[0] + 3;
System.out.println(potions[1]);','["10", "8", "6", "7"]'::jsonb,3,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-java-arrays-medium-03','java','arrays','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {5, 8, 10};
potions[1] = potions[0] + 4;
System.out.println(potions[1]);','["9", "10", "8", "12"]'::jsonb,0,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-java-arrays-medium-04','java','arrays','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {6, 9, 11};
potions[1] = potions[0] + 5;
System.out.println(potions[1]);','["12", "11", "10", "14"]'::jsonb,1,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-java-methods-medium-01','java','methods','medium',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int boost(int hp) {
    return hp * 2 + 1;
}
System.out.println(boost(3));','["10", "8", "6", "7"]'::jsonb,3,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-java-methods-medium-02','java','methods','medium',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int boost(int hp) {
    return hp * 2 + 1;
}
System.out.println(boost(4));','["9", "10", "8", "12"]'::jsonb,0,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-java-methods-medium-03','java','methods','medium',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int boost(int hp) {
    return hp * 2 + 1;
}
System.out.println(boost(5));','["12", "11", "10", "14"]'::jsonb,1,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-java-methods-medium-04','java','methods','medium',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int boost(int hp) {
    return hp * 2 + 1;
}
System.out.println(boost(6));','["12", "14", "13", "16"]'::jsonb,2,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-java-operators-hard-01','java','operators','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println((3 + 2) * 3 % 5);','["-1", "1", "0", "3"]'::jsonb,2,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-java-operators-hard-02','java','operators','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println((4 + 3) * 3 % 5);','["4", "2", "0", "1"]'::jsonb,3,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-java-operators-hard-03','java','operators','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println((5 + 4) * 3 % 5);','["2", "3", "1", "5"]'::jsonb,0,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-java-operators-hard-04','java','operators','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println((6 + 5) * 3 % 5);','["4", "3", "2", "6"]'::jsonb,1,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-java-strings-hard-01','java','strings','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println("Lantern".substring(1, 4) + "!");','["ant!!", "No output", "An error occurs", "ant!"]'::jsonb,3,'Text positions start at zero. Length counts all characters. A slice/substring selects part of the text; Python/Java stop before position 4, while C# takes 3 characters from position 1.',true,6),
('curriculum-v2-java-strings-hard-02','java','strings','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println("Warrior".substring(1, 4) + "!");','["arr!", "No output", "An error occurs", "arr!!"]'::jsonb,0,'Text positions start at zero. Length counts all characters. A slice/substring selects part of the text; Python/Java stop before position 4, while C# takes 3 characters from position 1.',true,6),
('curriculum-v2-java-strings-hard-03','java','strings','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println("Journey".substring(1, 4) + "!");','["No output", "our!", "An error occurs", "our!!"]'::jsonb,1,'Text positions start at zero. Length counts all characters. A slice/substring selects part of the text; Python/Java stop before position 4, while C# takes 3 characters from position 1.',true,6),
('curriculum-v2-java-strings-hard-04','java','strings','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','System.out.println("Crystal".substring(1, 4) + "!");','["An error occurs", "No output", "rys!", "rys!!"]'::jsonb,2,'Text positions start at zero. Length counts all characters. A slice/substring selects part of the text; Python/Java stop before position 4, while C# takes 3 characters from position 1.',true,6),
('curriculum-v2-java-conditionals-hard-01','java','conditionals','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 3;
int keys = 0;
if (coins >= 5) {
    if (keys >= 1) {
        System.out.println("Open");
    } else {
        System.out.println("Need key");
    }
} else {
    System.out.println("Need coins");
}','["No output", "Need coins", "An error occurs", "Need coins!"]'::jsonb,1,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-java-conditionals-hard-02','java','conditionals','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 4;
int keys = 1;
if (coins >= 5) {
    if (keys >= 1) {
        System.out.println("Open");
    } else {
        System.out.println("Need key");
    }
} else {
    System.out.println("Need coins");
}','["An error occurs", "No output", "Need coins", "Need coins!"]'::jsonb,2,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-java-conditionals-hard-03','java','conditionals','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 5;
int keys = 0;
if (coins >= 5) {
    if (keys >= 1) {
        System.out.println("Open");
    } else {
        System.out.println("Need key");
    }
} else {
    System.out.println("Need coins");
}','["Need key!", "No output", "An error occurs", "Need key"]'::jsonb,3,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-java-conditionals-hard-04','java','conditionals','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 6;
int keys = 1;
if (coins >= 5) {
    if (keys >= 1) {
        System.out.println("Open");
    } else {
        System.out.println("Need key");
    }
} else {
    System.out.println("Need coins");
}','["Open", "No output", "An error occurs", "Open!"]'::jsonb,0,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-java-loops-hard-01','java','loops','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int total = 0;
for (int i = 0; i < 2; i++) {
    total = total + i * (i + 1);
}
System.out.println(total);','["1", "3", "2", "5"]'::jsonb,2,'The counter starts at 0 and stops before 2. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-java-loops-hard-02','java','loops','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int total = 0;
for (int i = 0; i < 3; i++) {
    total = total + i * (i + 1);
}
System.out.println(total);','["11", "9", "7", "8"]'::jsonb,3,'The counter starts at 0 and stops before 3. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-java-loops-hard-03','java','loops','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int total = 0;
for (int i = 0; i < 4; i++) {
    total = total + i * (i + 1);
}
System.out.println(total);','["20", "21", "19", "23"]'::jsonb,0,'The counter starts at 0 and stops before 4. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-java-loops-hard-04','java','loops','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int total = 0;
for (int i = 0; i < 5; i++) {
    total = total + i * (i + 1);
}
System.out.println(total);','["41", "40", "39", "43"]'::jsonb,1,'The counter starts at 0 and stops before 5. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-java-arrays-hard-01','java','arrays','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {3, 6, 8};
int total = 0;
for (int i = 0; i < potions.length; i++) {
    if (potions[i] > 6) {
        total = total + potions[i];
    }
}
System.out.println(total);','["11", "9", "7", "8"]'::jsonb,3,'Shelf positions start at 0. Read or change the value at that position. In the loop, add only values that pass the > 6 test.',true,10),
('curriculum-v2-java-arrays-hard-02','java','arrays','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {4, 7, 9};
int total = 0;
for (int i = 0; i < potions.length; i++) {
    if (potions[i] > 6) {
        total = total + potions[i];
    }
}
System.out.println(total);','["16", "17", "15", "19"]'::jsonb,0,'Shelf positions start at 0. Read or change the value at that position. In the loop, add only values that pass the > 6 test.',true,10),
('curriculum-v2-java-arrays-hard-03','java','arrays','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {5, 8, 10};
int total = 0;
for (int i = 0; i < potions.length; i++) {
    if (potions[i] > 6) {
        total = total + potions[i];
    }
}
System.out.println(total);','["19", "18", "17", "21"]'::jsonb,1,'Shelf positions start at 0. Read or change the value at that position. In the loop, add only values that pass the > 6 test.',true,10),
('curriculum-v2-java-arrays-hard-04','java','arrays','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {6, 9, 11};
int total = 0;
for (int i = 0; i < potions.length; i++) {
    if (potions[i] > 6) {
        total = total + potions[i];
    }
}
System.out.println(total);','["19", "21", "20", "23"]'::jsonb,2,'Shelf positions start at 0. Read or change the value at that position. In the loop, add only values that pass the > 6 test.',true,10),
('curriculum-v2-java-methods-hard-01','java','methods','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int boost(int hp) {
    return hp * 2 + 1;
}
System.out.println(boost(boost(3)));','["15", "16", "14", "18"]'::jsonb,0,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-java-methods-hard-02','java','methods','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int boost(int hp) {
    return hp * 2 + 1;
}
System.out.println(boost(boost(4)));','["20", "19", "18", "22"]'::jsonb,1,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-java-methods-hard-03','java','methods','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int boost(int hp) {
    return hp * 2 + 1;
}
System.out.println(boost(boost(5)));','["22", "24", "23", "26"]'::jsonb,2,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-java-methods-hard-04','java','methods','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int boost(int hp) {
    return hp * 2 + 1;
}
System.out.println(boost(boost(6)));','["30", "28", "26", "27"]'::jsonb,3,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-csharp-output-easy-01','csharp','output','easy',1,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine("Lantern ready");','["Lantern ready", "No output", "An error occurs", "Lantern ready!"]'::jsonb,0,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-csharp-output-easy-02','csharp','output','easy',1,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine("Bridge repaired");','["No output", "Bridge repaired", "An error occurs", "Bridge repaired!"]'::jsonb,1,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-csharp-output-easy-03','csharp','output','easy',1,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine("Quest accepted");','["An error occurs", "No output", "Quest accepted", "Quest accepted!"]'::jsonb,2,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-csharp-output-easy-04','csharp','output','easy',1,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine("Shield raised");','["Shield raised!", "No output", "An error occurs", "Shield raised"]'::jsonb,3,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-csharp-syntax-easy-01','csharp','syntax','easy',1,null,'fill_blank','Fill the blank with the punctuation that makes this code valid.','int coins = 3___','[":", ";", "==", ","]'::jsonb,1,'Python starts an indented block with a colon. Java/C# end this assignment statement with a semicolon.',true,2),
('curriculum-v2-csharp-syntax-easy-02','csharp','syntax','easy',1,null,'fill_blank','Fill the blank with the punctuation that makes this code valid.','int coins = 4___','["==", ":", ";", ","]'::jsonb,2,'Python starts an indented block with a colon. Java/C# end this assignment statement with a semicolon.',true,2),
('curriculum-v2-csharp-syntax-easy-03','csharp','syntax','easy',1,null,'fill_blank','Fill the blank with the punctuation that makes this code valid.','int coins = 5___','[",", ":", "==", ";"]'::jsonb,3,'Python starts an indented block with a colon. Java/C# end this assignment statement with a semicolon.',true,2),
('curriculum-v2-csharp-syntax-easy-04','csharp','syntax','easy',1,null,'fill_blank','Fill the blank with the punctuation that makes this code valid.','int coins = 6___','[";", ":", "==", ","]'::jsonb,0,'Python starts an indented block with a colon. Java/C# end this assignment statement with a semicolon.',true,2),
('curriculum-v2-csharp-types-easy-01','csharp','types','easy',1,null,'multiple_choice','Which type name describes a whole number, such as 3?','','["bool", "string", "int", "double"]'::jsonb,2,'int is the type name for whole numbers, such as 3 or 12.',true,3),
('curriculum-v2-csharp-types-easy-02','csharp','types','easy',1,null,'multiple_choice','Which type name describes a whole number, such as 4?','','["double", "string", "bool", "int"]'::jsonb,3,'int is the type name for whole numbers, such as 3 or 12.',true,3),
('curriculum-v2-csharp-types-easy-03','csharp','types','easy',1,null,'multiple_choice','Which type name describes a whole number, such as 5?','','["int", "string", "bool", "double"]'::jsonb,0,'int is the type name for whole numbers, such as 3 or 12.',true,3),
('curriculum-v2-csharp-types-easy-04','csharp','types','easy',1,null,'multiple_choice','Which type name describes a whole number, such as 6?','','["string", "int", "bool", "double"]'::jsonb,1,'int is the type name for whole numbers, such as 3 or 12.',true,3),
('curriculum-v2-csharp-variables-easy-01','csharp','variables','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 3;
Console.WriteLine(coins);','["6", "4", "2", "3"]'::jsonb,3,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-csharp-variables-easy-02','csharp','variables','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 4;
Console.WriteLine(coins);','["4", "5", "3", "7"]'::jsonb,0,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-csharp-variables-easy-03','csharp','variables','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 5;
Console.WriteLine(coins);','["6", "5", "4", "8"]'::jsonb,1,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-csharp-variables-easy-04','csharp','variables','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 6;
Console.WriteLine(coins);','["5", "7", "6", "9"]'::jsonb,2,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-csharp-operators-easy-01','csharp','operators','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(3 + 2);','["5", "6", "4", "8"]'::jsonb,0,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-csharp-operators-easy-02','csharp','operators','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(4 + 3);','["8", "7", "6", "10"]'::jsonb,1,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-csharp-operators-easy-03','csharp','operators','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(5 + 4);','["8", "10", "9", "12"]'::jsonb,2,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-csharp-operators-easy-04','csharp','operators','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(6 + 5);','["14", "12", "10", "11"]'::jsonb,3,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-csharp-strings-easy-01','csharp','strings','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine("Hi " + "Lantern");','["No output", "Hi Lantern", "An error occurs", "Hi Lantern!"]'::jsonb,1,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-csharp-strings-easy-02','csharp','strings','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine("Hi " + "Warrior");','["An error occurs", "No output", "Hi Warrior", "Hi Warrior!"]'::jsonb,2,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-csharp-strings-easy-03','csharp','strings','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine("Hi " + "Journey");','["Hi Journey!", "No output", "An error occurs", "Hi Journey"]'::jsonb,3,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-csharp-strings-easy-04','csharp','strings','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine("Hi " + "Crystal");','["Hi Crystal", "No output", "An error occurs", "Hi Crystal!"]'::jsonb,0,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-csharp-comparisons-easy-01','csharp','comparisons','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(3 > 2);','["No output", "False", "True", "An error occurs"]'::jsonb,2,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-csharp-comparisons-easy-02','csharp','comparisons','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(3 >= 4);','["An error occurs", "True", "No output", "False"]'::jsonb,3,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-csharp-comparisons-easy-03','csharp','comparisons','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(5 > 4);','["True", "False", "No output", "An error occurs"]'::jsonb,0,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-csharp-comparisons-easy-04','csharp','comparisons','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(5 >= 6);','["True", "False", "No output", "An error occurs"]'::jsonb,1,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-csharp-conditionals-easy-01','csharp','conditionals','easy',4,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 3;
if (coins >= 5) {
    Console.WriteLine("Open");
} else {
    Console.WriteLine("Locked");
}','["Locked!", "No output", "An error occurs", "Locked"]'::jsonb,3,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-csharp-conditionals-easy-02','csharp','conditionals','easy',4,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 4;
if (coins >= 5) {
    Console.WriteLine("Open");
} else {
    Console.WriteLine("Locked");
}','["Locked", "No output", "An error occurs", "Locked!"]'::jsonb,0,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-csharp-conditionals-easy-03','csharp','conditionals','easy',4,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 5;
if (coins >= 5) {
    Console.WriteLine("Open");
} else {
    Console.WriteLine("Locked");
}','["No output", "Open", "An error occurs", "Open!"]'::jsonb,1,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-csharp-conditionals-easy-04','csharp','conditionals','easy',4,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 6;
if (coins >= 5) {
    Console.WriteLine("Open");
} else {
    Console.WriteLine("Locked");
}','["An error occurs", "No output", "Open", "Open!"]'::jsonb,2,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-csharp-loops-easy-01','csharp','loops','easy',5,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','for (int i = 0; i < 2; i++) {
    Console.WriteLine(i);
}','["0\n1", "No output", "An error occurs", "0\n1!"]'::jsonb,0,'The counter starts at 0 and stops before 2. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-csharp-loops-easy-02','csharp','loops','easy',5,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','for (int i = 0; i < 3; i++) {
    Console.WriteLine(i);
}','["No output", "0\n1\n2", "An error occurs", "0\n1\n2!"]'::jsonb,1,'The counter starts at 0 and stops before 3. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-csharp-loops-easy-03','csharp','loops','easy',5,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','for (int i = 0; i < 4; i++) {
    Console.WriteLine(i);
}','["An error occurs", "No output", "0\n1\n2\n3", "0\n1\n2\n3!"]'::jsonb,2,'The counter starts at 0 and stops before 4. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-csharp-loops-easy-04','csharp','loops','easy',5,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','for (int i = 0; i < 5; i++) {
    Console.WriteLine(i);
}','["0\n1\n2\n3\n4!", "No output", "An error occurs", "0\n1\n2\n3\n4"]'::jsonb,3,'The counter starts at 0 and stops before 5. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-csharp-arrays-easy-01','csharp','arrays','easy',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {3, 6, 8};
Console.WriteLine(potions[0]);','["4", "3", "2", "6"]'::jsonb,1,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-csharp-arrays-easy-02','csharp','arrays','easy',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {4, 7, 9};
Console.WriteLine(potions[1]);','["6", "8", "7", "10"]'::jsonb,2,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-csharp-arrays-easy-03','csharp','arrays','easy',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {5, 8, 10};
Console.WriteLine(potions[2]);','["13", "11", "9", "10"]'::jsonb,3,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-csharp-arrays-easy-04','csharp','arrays','easy',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {6, 9, 11};
Console.WriteLine(potions[0]);','["6", "7", "5", "9"]'::jsonb,0,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-csharp-methods-easy-01','csharp','methods','easy',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int Boost(int hp) {
    return hp + 2;
}
Console.WriteLine(Boost(3));','["4", "6", "5", "8"]'::jsonb,2,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-csharp-methods-easy-02','csharp','methods','easy',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int Boost(int hp) {
    return hp + 2;
}
Console.WriteLine(Boost(4));','["9", "7", "5", "6"]'::jsonb,3,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-csharp-methods-easy-03','csharp','methods','easy',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int Boost(int hp) {
    return hp + 2;
}
Console.WriteLine(Boost(5));','["7", "8", "6", "10"]'::jsonb,0,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-csharp-methods-easy-04','csharp','methods','easy',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int Boost(int hp) {
    return hp + 2;
}
Console.WriteLine(Boost(6));','["9", "8", "7", "11"]'::jsonb,1,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-csharp-output-medium-01','csharp','output','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(3 + 2);
Console.WriteLine("Ready");','["No output", "5\nReady", "An error occurs", "5\nReady!"]'::jsonb,1,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-csharp-output-medium-02','csharp','output','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(4 + 3);
Console.WriteLine("Ready");','["An error occurs", "No output", "7\nReady", "7\nReady!"]'::jsonb,2,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-csharp-output-medium-03','csharp','output','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(5 + 4);
Console.WriteLine("Ready");','["9\nReady!", "No output", "An error occurs", "9\nReady"]'::jsonb,3,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-csharp-output-medium-04','csharp','output','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(6 + 5);
Console.WriteLine("Ready");','["11\nReady", "No output", "An error occurs", "11\nReady!"]'::jsonb,0,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-csharp-variables-medium-01','csharp','variables','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 3;
coins = coins + 2;
Console.WriteLine(coins);','["5", "6", "4", "8"]'::jsonb,0,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-csharp-variables-medium-02','csharp','variables','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 4;
coins = coins + 3;
Console.WriteLine(coins);','["8", "7", "6", "10"]'::jsonb,1,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-csharp-variables-medium-03','csharp','variables','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 5;
coins = coins + 4;
Console.WriteLine(coins);','["8", "10", "9", "12"]'::jsonb,2,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-csharp-variables-medium-04','csharp','variables','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 6;
coins = coins + 5;
Console.WriteLine(coins);','["14", "12", "10", "11"]'::jsonb,3,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-csharp-operators-medium-01','csharp','operators','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(3 * 2 - 1);','["6", "5", "4", "8"]'::jsonb,1,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-csharp-operators-medium-02','csharp','operators','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(4 * 3 - 1);','["10", "12", "11", "14"]'::jsonb,2,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-csharp-operators-medium-03','csharp','operators','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(5 * 4 - 1);','["22", "20", "18", "19"]'::jsonb,3,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-csharp-operators-medium-04','csharp','operators','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(6 * 5 - 1);','["29", "30", "28", "32"]'::jsonb,0,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-csharp-strings-medium-01','csharp','strings','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine("Lantern".Length);','["6", "8", "7", "10"]'::jsonb,2,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-csharp-strings-medium-02','csharp','strings','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine("Warrior".Length);','["10", "8", "6", "7"]'::jsonb,3,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-csharp-strings-medium-03','csharp','strings','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine("Journey".Length);','["7", "8", "6", "10"]'::jsonb,0,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-csharp-strings-medium-04','csharp','strings','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine("Crystal".Length);','["8", "7", "6", "10"]'::jsonb,1,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-csharp-comparisons-medium-01','csharp','comparisons','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(3 > 2 && 3 < 10);','["An error occurs", "False", "No output", "True"]'::jsonb,3,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-csharp-comparisons-medium-02','csharp','comparisons','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(3 >= 4 && 4 < 10);','["False", "True", "No output", "An error occurs"]'::jsonb,0,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-csharp-comparisons-medium-03','csharp','comparisons','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(5 > 4 && 5 < 10);','["False", "True", "No output", "An error occurs"]'::jsonb,1,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-csharp-comparisons-medium-04','csharp','comparisons','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine(5 >= 6 && 6 < 10);','["No output", "True", "False", "An error occurs"]'::jsonb,2,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-csharp-conditionals-medium-01','csharp','conditionals','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 3;
int keys = 0;
if (coins >= 5 && keys >= 1) {
    Console.WriteLine("Open");
} else {
    Console.WriteLine("Locked");
}','["Locked", "No output", "An error occurs", "Locked!"]'::jsonb,0,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-csharp-conditionals-medium-02','csharp','conditionals','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 4;
int keys = 1;
if (coins >= 5 && keys >= 1) {
    Console.WriteLine("Open");
} else {
    Console.WriteLine("Locked");
}','["No output", "Locked", "An error occurs", "Locked!"]'::jsonb,1,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-csharp-conditionals-medium-03','csharp','conditionals','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 5;
int keys = 0;
if (coins >= 5 && keys >= 1) {
    Console.WriteLine("Open");
} else {
    Console.WriteLine("Locked");
}','["An error occurs", "No output", "Locked", "Locked!"]'::jsonb,2,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-csharp-conditionals-medium-04','csharp','conditionals','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 6;
int keys = 1;
if (coins >= 5 && keys >= 1) {
    Console.WriteLine("Open");
} else {
    Console.WriteLine("Locked");
}','["Open!", "No output", "An error occurs", "Open"]'::jsonb,3,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-csharp-loops-medium-01','csharp','loops','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int total = 0;
for (int i = 0; i < 2; i++) {
    total = total + 2;
}
Console.WriteLine(total);','["5", "4", "3", "7"]'::jsonb,1,'The counter starts at 0 and stops before 2. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-csharp-loops-medium-02','csharp','loops','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int total = 0;
for (int i = 0; i < 3; i++) {
    total = total + 2;
}
Console.WriteLine(total);','["5", "7", "6", "9"]'::jsonb,2,'The counter starts at 0 and stops before 3. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-csharp-loops-medium-03','csharp','loops','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int total = 0;
for (int i = 0; i < 4; i++) {
    total = total + 2;
}
Console.WriteLine(total);','["11", "9", "7", "8"]'::jsonb,3,'The counter starts at 0 and stops before 4. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-csharp-loops-medium-04','csharp','loops','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int total = 0;
for (int i = 0; i < 5; i++) {
    total = total + 2;
}
Console.WriteLine(total);','["10", "11", "9", "13"]'::jsonb,0,'The counter starts at 0 and stops before 5. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-csharp-arrays-medium-01','csharp','arrays','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {3, 6, 8};
potions[1] = potions[0] + 2;
Console.WriteLine(potions[1]);','["4", "6", "5", "8"]'::jsonb,2,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-csharp-arrays-medium-02','csharp','arrays','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {4, 7, 9};
potions[1] = potions[0] + 3;
Console.WriteLine(potions[1]);','["10", "8", "6", "7"]'::jsonb,3,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-csharp-arrays-medium-03','csharp','arrays','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {5, 8, 10};
potions[1] = potions[0] + 4;
Console.WriteLine(potions[1]);','["9", "10", "8", "12"]'::jsonb,0,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-csharp-arrays-medium-04','csharp','arrays','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {6, 9, 11};
potions[1] = potions[0] + 5;
Console.WriteLine(potions[1]);','["12", "11", "10", "14"]'::jsonb,1,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-csharp-methods-medium-01','csharp','methods','medium',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int Boost(int hp) {
    return hp * 2 + 1;
}
Console.WriteLine(Boost(3));','["10", "8", "6", "7"]'::jsonb,3,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-csharp-methods-medium-02','csharp','methods','medium',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int Boost(int hp) {
    return hp * 2 + 1;
}
Console.WriteLine(Boost(4));','["9", "10", "8", "12"]'::jsonb,0,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-csharp-methods-medium-03','csharp','methods','medium',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int Boost(int hp) {
    return hp * 2 + 1;
}
Console.WriteLine(Boost(5));','["12", "11", "10", "14"]'::jsonb,1,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-csharp-methods-medium-04','csharp','methods','medium',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int Boost(int hp) {
    return hp * 2 + 1;
}
Console.WriteLine(Boost(6));','["12", "14", "13", "16"]'::jsonb,2,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-csharp-operators-hard-01','csharp','operators','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine((3 + 2) * 3 % 5);','["-1", "1", "0", "3"]'::jsonb,2,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-csharp-operators-hard-02','csharp','operators','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine((4 + 3) * 3 % 5);','["4", "2", "0", "1"]'::jsonb,3,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-csharp-operators-hard-03','csharp','operators','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine((5 + 4) * 3 % 5);','["2", "3", "1", "5"]'::jsonb,0,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-csharp-operators-hard-04','csharp','operators','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine((6 + 5) * 3 % 5);','["4", "3", "2", "6"]'::jsonb,1,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-csharp-strings-hard-01','csharp','strings','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine("Lantern".Substring(1, 3) + "!");','["ant!!", "No output", "An error occurs", "ant!"]'::jsonb,3,'Text positions start at zero. Length counts all characters. A slice/substring selects part of the text; Python/Java stop before position 4, while C# takes 3 characters from position 1.',true,6),
('curriculum-v2-csharp-strings-hard-02','csharp','strings','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine("Warrior".Substring(1, 3) + "!");','["arr!", "No output", "An error occurs", "arr!!"]'::jsonb,0,'Text positions start at zero. Length counts all characters. A slice/substring selects part of the text; Python/Java stop before position 4, while C# takes 3 characters from position 1.',true,6),
('curriculum-v2-csharp-strings-hard-03','csharp','strings','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine("Journey".Substring(1, 3) + "!");','["No output", "our!", "An error occurs", "our!!"]'::jsonb,1,'Text positions start at zero. Length counts all characters. A slice/substring selects part of the text; Python/Java stop before position 4, while C# takes 3 characters from position 1.',true,6),
('curriculum-v2-csharp-strings-hard-04','csharp','strings','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','Console.WriteLine("Crystal".Substring(1, 3) + "!");','["An error occurs", "No output", "rys!", "rys!!"]'::jsonb,2,'Text positions start at zero. Length counts all characters. A slice/substring selects part of the text; Python/Java stop before position 4, while C# takes 3 characters from position 1.',true,6),
('curriculum-v2-csharp-conditionals-hard-01','csharp','conditionals','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 3;
int keys = 0;
if (coins >= 5) {
    if (keys >= 1) {
        Console.WriteLine("Open");
    } else {
        Console.WriteLine("Need key");
    }
} else {
    Console.WriteLine("Need coins");
}','["No output", "Need coins", "An error occurs", "Need coins!"]'::jsonb,1,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-csharp-conditionals-hard-02','csharp','conditionals','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 4;
int keys = 1;
if (coins >= 5) {
    if (keys >= 1) {
        Console.WriteLine("Open");
    } else {
        Console.WriteLine("Need key");
    }
} else {
    Console.WriteLine("Need coins");
}','["An error occurs", "No output", "Need coins", "Need coins!"]'::jsonb,2,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-csharp-conditionals-hard-03','csharp','conditionals','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 5;
int keys = 0;
if (coins >= 5) {
    if (keys >= 1) {
        Console.WriteLine("Open");
    } else {
        Console.WriteLine("Need key");
    }
} else {
    Console.WriteLine("Need coins");
}','["Need key!", "No output", "An error occurs", "Need key"]'::jsonb,3,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-csharp-conditionals-hard-04','csharp','conditionals','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int coins = 6;
int keys = 1;
if (coins >= 5) {
    if (keys >= 1) {
        Console.WriteLine("Open");
    } else {
        Console.WriteLine("Need key");
    }
} else {
    Console.WriteLine("Need coins");
}','["Open", "No output", "An error occurs", "Open!"]'::jsonb,0,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-csharp-loops-hard-01','csharp','loops','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int total = 0;
for (int i = 0; i < 2; i++) {
    total = total + i * (i + 1);
}
Console.WriteLine(total);','["1", "3", "2", "5"]'::jsonb,2,'The counter starts at 0 and stops before 2. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-csharp-loops-hard-02','csharp','loops','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int total = 0;
for (int i = 0; i < 3; i++) {
    total = total + i * (i + 1);
}
Console.WriteLine(total);','["11", "9", "7", "8"]'::jsonb,3,'The counter starts at 0 and stops before 3. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-csharp-loops-hard-03','csharp','loops','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int total = 0;
for (int i = 0; i < 4; i++) {
    total = total + i * (i + 1);
}
Console.WriteLine(total);','["20", "21", "19", "23"]'::jsonb,0,'The counter starts at 0 and stops before 4. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-csharp-loops-hard-04','csharp','loops','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int total = 0;
for (int i = 0; i < 5; i++) {
    total = total + i * (i + 1);
}
Console.WriteLine(total);','["41", "40", "39", "43"]'::jsonb,1,'The counter starts at 0 and stops before 5. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-csharp-arrays-hard-01','csharp','arrays','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {3, 6, 8};
int total = 0;
for (int i = 0; i < potions.Length; i++) {
    if (potions[i] > 6) {
        total = total + potions[i];
    }
}
Console.WriteLine(total);','["11", "9", "7", "8"]'::jsonb,3,'Shelf positions start at 0. Read or change the value at that position. In the loop, add only values that pass the > 6 test.',true,10),
('curriculum-v2-csharp-arrays-hard-02','csharp','arrays','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {4, 7, 9};
int total = 0;
for (int i = 0; i < potions.Length; i++) {
    if (potions[i] > 6) {
        total = total + potions[i];
    }
}
Console.WriteLine(total);','["16", "17", "15", "19"]'::jsonb,0,'Shelf positions start at 0. Read or change the value at that position. In the loop, add only values that pass the > 6 test.',true,10),
('curriculum-v2-csharp-arrays-hard-03','csharp','arrays','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {5, 8, 10};
int total = 0;
for (int i = 0; i < potions.Length; i++) {
    if (potions[i] > 6) {
        total = total + potions[i];
    }
}
Console.WriteLine(total);','["19", "18", "17", "21"]'::jsonb,1,'Shelf positions start at 0. Read or change the value at that position. In the loop, add only values that pass the > 6 test.',true,10),
('curriculum-v2-csharp-arrays-hard-04','csharp','arrays','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','int[] potions = {6, 9, 11};
int total = 0;
for (int i = 0; i < potions.Length; i++) {
    if (potions[i] > 6) {
        total = total + potions[i];
    }
}
Console.WriteLine(total);','["19", "21", "20", "23"]'::jsonb,2,'Shelf positions start at 0. Read or change the value at that position. In the loop, add only values that pass the > 6 test.',true,10),
('curriculum-v2-csharp-methods-hard-01','csharp','methods','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int Boost(int hp) {
    return hp * 2 + 1;
}
Console.WriteLine(Boost(Boost(3)));','["15", "16", "14", "18"]'::jsonb,0,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-csharp-methods-hard-02','csharp','methods','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int Boost(int hp) {
    return hp * 2 + 1;
}
Console.WriteLine(Boost(Boost(4)));','["20", "19", "18", "22"]'::jsonb,1,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-csharp-methods-hard-03','csharp','methods','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int Boost(int hp) {
    return hp * 2 + 1;
}
Console.WriteLine(Boost(Boost(5)));','["22", "24", "23", "26"]'::jsonb,2,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-csharp-methods-hard-04','csharp','methods','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','static int Boost(int hp) {
    return hp * 2 + 1;
}
Console.WriteLine(Boost(Boost(6)));','["30", "28", "26", "27"]'::jsonb,3,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-python-output-easy-01','python','output','easy',1,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print("Lantern ready")','["Lantern ready", "No output", "An error occurs", "Lantern ready!"]'::jsonb,0,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-python-output-easy-02','python','output','easy',1,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print("Bridge repaired")','["No output", "Bridge repaired", "An error occurs", "Bridge repaired!"]'::jsonb,1,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-python-output-easy-03','python','output','easy',1,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print("Quest accepted")','["An error occurs", "No output", "Quest accepted", "Quest accepted!"]'::jsonb,2,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-python-output-easy-04','python','output','easy',1,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print("Shield raised")','["Shield raised!", "No output", "An error occurs", "Shield raised"]'::jsonb,3,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-python-syntax-easy-01','python','syntax','easy',1,null,'fill_blank','Fill the blank with the punctuation that makes this code valid.','if 3 > 2___
    print("Ready 1")','[",", ":", ";", "."]'::jsonb,1,'Python starts an indented block with a colon. Java/C# end this assignment statement with a semicolon.',true,2),
('curriculum-v2-python-syntax-easy-02','python','syntax','easy',1,null,'fill_blank','Fill the blank with the punctuation that makes this code valid.','if 4 > 3___
    print("Ready 2")','[";", ",", ":", "."]'::jsonb,2,'Python starts an indented block with a colon. Java/C# end this assignment statement with a semicolon.',true,2),
('curriculum-v2-python-syntax-easy-03','python','syntax','easy',1,null,'fill_blank','Fill the blank with the punctuation that makes this code valid.','if 5 > 4___
    print("Ready 3")','[".", ",", ";", ":"]'::jsonb,3,'Python starts an indented block with a colon. Java/C# end this assignment statement with a semicolon.',true,2),
('curriculum-v2-python-syntax-easy-04','python','syntax','easy',1,null,'fill_blank','Fill the blank with the punctuation that makes this code valid.','if 6 > 5___
    print("Ready 4")','[":", ",", ";", "."]'::jsonb,0,'Python starts an indented block with a colon. Java/C# end this assignment statement with a semicolon.',true,2),
('curriculum-v2-python-types-easy-01','python','types','easy',1,null,'multiple_choice','Which type name describes a whole number, such as 3?','','["bool", "str", "int", "float"]'::jsonb,2,'int is the type name for whole numbers, such as 3 or 12.',true,3),
('curriculum-v2-python-types-easy-02','python','types','easy',1,null,'multiple_choice','Which type name describes a whole number, such as 4?','','["float", "str", "bool", "int"]'::jsonb,3,'int is the type name for whole numbers, such as 3 or 12.',true,3),
('curriculum-v2-python-types-easy-03','python','types','easy',1,null,'multiple_choice','Which type name describes a whole number, such as 5?','','["int", "str", "bool", "float"]'::jsonb,0,'int is the type name for whole numbers, such as 3 or 12.',true,3),
('curriculum-v2-python-types-easy-04','python','types','easy',1,null,'multiple_choice','Which type name describes a whole number, such as 6?','','["str", "int", "bool", "float"]'::jsonb,1,'int is the type name for whole numbers, such as 3 or 12.',true,3),
('curriculum-v2-python-variables-easy-01','python','variables','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 3
print(coins)','["6", "4", "2", "3"]'::jsonb,3,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-python-variables-easy-02','python','variables','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 4
print(coins)','["4", "5", "3", "7"]'::jsonb,0,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-python-variables-easy-03','python','variables','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 5
print(coins)','["6", "5", "4", "8"]'::jsonb,1,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-python-variables-easy-04','python','variables','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 6
print(coins)','["5", "7", "6", "9"]'::jsonb,2,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-python-operators-easy-01','python','operators','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(3 + 2)','["5", "6", "4", "8"]'::jsonb,0,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-python-operators-easy-02','python','operators','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(4 + 3)','["8", "7", "6", "10"]'::jsonb,1,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-python-operators-easy-03','python','operators','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(5 + 4)','["8", "10", "9", "12"]'::jsonb,2,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-python-operators-easy-04','python','operators','easy',2,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(6 + 5)','["14", "12", "10", "11"]'::jsonb,3,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-python-strings-easy-01','python','strings','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print("Hi " + "Lantern")','["No output", "Hi Lantern", "An error occurs", "Hi Lantern!"]'::jsonb,1,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-python-strings-easy-02','python','strings','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print("Hi " + "Warrior")','["An error occurs", "No output", "Hi Warrior", "Hi Warrior!"]'::jsonb,2,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-python-strings-easy-03','python','strings','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print("Hi " + "Journey")','["Hi Journey!", "No output", "An error occurs", "Hi Journey"]'::jsonb,3,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-python-strings-easy-04','python','strings','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print("Hi " + "Crystal")','["Hi Crystal", "No output", "An error occurs", "Hi Crystal!"]'::jsonb,0,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-python-comparisons-easy-01','python','comparisons','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(3 > 2)','["No output", "False", "True", "An error occurs"]'::jsonb,2,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-python-comparisons-easy-02','python','comparisons','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(3 >= 4)','["An error occurs", "True", "No output", "False"]'::jsonb,3,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-python-comparisons-easy-03','python','comparisons','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(5 > 4)','["True", "False", "No output", "An error occurs"]'::jsonb,0,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-python-comparisons-easy-04','python','comparisons','easy',3,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(5 >= 6)','["True", "False", "No output", "An error occurs"]'::jsonb,1,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-python-conditionals-easy-01','python','conditionals','easy',4,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 3
if coins >= 5:
    print("Open")
else:
    print("Locked")','["Locked!", "No output", "An error occurs", "Locked"]'::jsonb,3,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-python-conditionals-easy-02','python','conditionals','easy',4,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 4
if coins >= 5:
    print("Open")
else:
    print("Locked")','["Locked", "No output", "An error occurs", "Locked!"]'::jsonb,0,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-python-conditionals-easy-03','python','conditionals','easy',4,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 5
if coins >= 5:
    print("Open")
else:
    print("Locked")','["No output", "Open", "An error occurs", "Open!"]'::jsonb,1,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-python-conditionals-easy-04','python','conditionals','easy',4,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 6
if coins >= 5:
    print("Open")
else:
    print("Locked")','["An error occurs", "No output", "Open", "Open!"]'::jsonb,2,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-python-loops-easy-01','python','loops','easy',5,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','for i in range(2):
    print(i)','["0\n1", "No output", "An error occurs", "0\n1!"]'::jsonb,0,'The counter starts at 0 and stops before 2. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-python-loops-easy-02','python','loops','easy',5,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','for i in range(3):
    print(i)','["No output", "0\n1\n2", "An error occurs", "0\n1\n2!"]'::jsonb,1,'The counter starts at 0 and stops before 3. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-python-loops-easy-03','python','loops','easy',5,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','for i in range(4):
    print(i)','["An error occurs", "No output", "0\n1\n2\n3", "0\n1\n2\n3!"]'::jsonb,2,'The counter starts at 0 and stops before 4. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-python-loops-easy-04','python','loops','easy',5,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','for i in range(5):
    print(i)','["0\n1\n2\n3\n4!", "No output", "An error occurs", "0\n1\n2\n3\n4"]'::jsonb,3,'The counter starts at 0 and stops before 5. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-python-arrays-easy-01','python','arrays','easy',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','potions = [3, 6, 8]
print(potions[0])','["4", "3", "2", "6"]'::jsonb,1,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-python-arrays-easy-02','python','arrays','easy',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','potions = [4, 7, 9]
print(potions[1])','["6", "8", "7", "10"]'::jsonb,2,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-python-arrays-easy-03','python','arrays','easy',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','potions = [5, 8, 10]
print(potions[2])','["13", "11", "9", "10"]'::jsonb,3,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-python-arrays-easy-04','python','arrays','easy',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','potions = [6, 9, 11]
print(potions[0])','["6", "7", "5", "9"]'::jsonb,0,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-python-methods-easy-01','python','methods','easy',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','def boost(hp):
    return hp + 2
print(boost(3))','["4", "6", "5", "8"]'::jsonb,2,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-python-methods-easy-02','python','methods','easy',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','def boost(hp):
    return hp + 2
print(boost(4))','["9", "7", "5", "6"]'::jsonb,3,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-python-methods-easy-03','python','methods','easy',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','def boost(hp):
    return hp + 2
print(boost(5))','["7", "8", "6", "10"]'::jsonb,0,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-python-methods-easy-04','python','methods','easy',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','def boost(hp):
    return hp + 2
print(boost(6))','["9", "8", "7", "11"]'::jsonb,1,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-python-output-medium-01','python','output','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(3 + 2)
print("Ready")','["No output", "5\nReady", "An error occurs", "5\nReady!"]'::jsonb,1,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-python-output-medium-02','python','output','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(4 + 3)
print("Ready")','["An error occurs", "No output", "7\nReady", "7\nReady!"]'::jsonb,2,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-python-output-medium-03','python','output','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(5 + 4)
print("Ready")','["9\nReady!", "No output", "An error occurs", "9\nReady"]'::jsonb,3,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-python-output-medium-04','python','output','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(6 + 5)
print("Ready")','["11\nReady", "No output", "An error occurs", "11\nReady!"]'::jsonb,0,'Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.',true,1),
('curriculum-v2-python-variables-medium-01','python','variables','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 3
coins = coins + 2
print(coins)','["5", "6", "4", "8"]'::jsonb,0,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-python-variables-medium-02','python','variables','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 4
coins = coins + 3
print(coins)','["8", "7", "6", "10"]'::jsonb,1,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-python-variables-medium-03','python','variables','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 5
coins = coins + 4
print(coins)','["8", "10", "9", "12"]'::jsonb,2,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-python-variables-medium-04','python','variables','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 6
coins = coins + 5
print(coins)','["14", "12", "10", "11"]'::jsonb,3,'coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.',true,4),
('curriculum-v2-python-operators-medium-01','python','operators','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(3 * 2 - 1)','["6", "5", "4", "8"]'::jsonb,1,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-python-operators-medium-02','python','operators','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(4 * 3 - 1)','["10", "12", "11", "14"]'::jsonb,2,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-python-operators-medium-03','python','operators','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(5 * 4 - 1)','["22", "20", "18", "19"]'::jsonb,3,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-python-operators-medium-04','python','operators','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(6 * 5 - 1)','["29", "30", "28", "32"]'::jsonb,0,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-python-strings-medium-01','python','strings','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(len("Lantern"))','["6", "8", "7", "10"]'::jsonb,2,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-python-strings-medium-02','python','strings','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(len("Warrior"))','["10", "8", "6", "7"]'::jsonb,3,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-python-strings-medium-03','python','strings','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(len("Journey"))','["7", "8", "6", "10"]'::jsonb,0,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-python-strings-medium-04','python','strings','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(len("Crystal"))','["8", "7", "6", "10"]'::jsonb,1,'Joining text keeps its order and spaces. Length counts characters, including any spaces.',true,6),
('curriculum-v2-python-comparisons-medium-01','python','comparisons','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(3 > 2 and 3 < 10)','["An error occurs", "False", "No output", "True"]'::jsonb,3,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-python-comparisons-medium-02','python','comparisons','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(3 >= 4 and 4 < 10)','["False", "True", "No output", "An error occurs"]'::jsonb,0,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-python-comparisons-medium-03','python','comparisons','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(5 > 4 and 5 < 10)','["False", "True", "No output", "An error occurs"]'::jsonb,1,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-python-comparisons-medium-04','python','comparisons','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print(5 >= 6 and 6 < 10)','["No output", "True", "False", "An error occurs"]'::jsonb,2,'A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.',true,7),
('curriculum-v2-python-conditionals-medium-01','python','conditionals','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 3
keys = 0
if coins >= 5 and keys >= 1:
    print("Open")
else:
    print("Locked")','["Locked", "No output", "An error occurs", "Locked!"]'::jsonb,0,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-python-conditionals-medium-02','python','conditionals','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 4
keys = 1
if coins >= 5 and keys >= 1:
    print("Open")
else:
    print("Locked")','["No output", "Locked", "An error occurs", "Locked!"]'::jsonb,1,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-python-conditionals-medium-03','python','conditionals','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 5
keys = 0
if coins >= 5 and keys >= 1:
    print("Open")
else:
    print("Locked")','["An error occurs", "No output", "Locked", "Locked!"]'::jsonb,2,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-python-conditionals-medium-04','python','conditionals','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 6
keys = 1
if coins >= 5 and keys >= 1:
    print("Open")
else:
    print("Locked")','["Open!", "No output", "An error occurs", "Open"]'::jsonb,3,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-python-loops-medium-01','python','loops','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','total = 0
for i in range(2):
    total = total + 2
print(total)','["5", "4", "3", "7"]'::jsonb,1,'The counter starts at 0 and stops before 2. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-python-loops-medium-02','python','loops','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','total = 0
for i in range(3):
    total = total + 2
print(total)','["5", "7", "6", "9"]'::jsonb,2,'The counter starts at 0 and stops before 3. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-python-loops-medium-03','python','loops','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','total = 0
for i in range(4):
    total = total + 2
print(total)','["11", "9", "7", "8"]'::jsonb,3,'The counter starts at 0 and stops before 4. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-python-loops-medium-04','python','loops','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','total = 0
for i in range(5):
    total = total + 2
print(total)','["10", "11", "9", "13"]'::jsonb,0,'The counter starts at 0 and stops before 5. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-python-arrays-medium-01','python','arrays','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','potions = [3, 6, 8]
potions[1] = potions[0] + 2
print(potions[1])','["4", "6", "5", "8"]'::jsonb,2,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-python-arrays-medium-02','python','arrays','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','potions = [4, 7, 9]
potions[1] = potions[0] + 3
print(potions[1])','["10", "8", "6", "7"]'::jsonb,3,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-python-arrays-medium-03','python','arrays','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','potions = [5, 8, 10]
potions[1] = potions[0] + 4
print(potions[1])','["9", "10", "8", "12"]'::jsonb,0,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-python-arrays-medium-04','python','arrays','medium',6,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','potions = [6, 9, 11]
potions[1] = potions[0] + 5
print(potions[1])','["12", "11", "10", "14"]'::jsonb,1,'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.',true,10),
('curriculum-v2-python-methods-medium-01','python','methods','medium',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','def boost(hp):
    return hp * 2 + 1
print(boost(3))','["10", "8", "6", "7"]'::jsonb,3,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-python-methods-medium-02','python','methods','medium',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','def boost(hp):
    return hp * 2 + 1
print(boost(4))','["9", "10", "8", "12"]'::jsonb,0,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-python-methods-medium-03','python','methods','medium',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','def boost(hp):
    return hp * 2 + 1
print(boost(5))','["12", "11", "10", "14"]'::jsonb,1,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-python-methods-medium-04','python','methods','medium',8,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','def boost(hp):
    return hp * 2 + 1
print(boost(6))','["12", "14", "13", "16"]'::jsonb,2,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-python-operators-hard-01','python','operators','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print((3 + 2) * 3 % 5)','["-1", "1", "0", "3"]'::jsonb,2,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-python-operators-hard-02','python','operators','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print((4 + 3) * 3 % 5)','["4", "2", "0", "1"]'::jsonb,3,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-python-operators-hard-03','python','operators','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print((5 + 4) * 3 % 5)','["2", "3", "1", "5"]'::jsonb,0,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-python-operators-hard-04','python','operators','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print((6 + 5) * 3 % 5)','["4", "3", "2", "6"]'::jsonb,1,'Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.',true,5),
('curriculum-v2-python-strings-hard-01','python','strings','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print("Lantern"[1:4] + "!")','["ant!!", "No output", "An error occurs", "ant!"]'::jsonb,3,'Text positions start at zero. Length counts all characters. A slice/substring selects part of the text; Python/Java stop before position 4, while C# takes 3 characters from position 1.',true,6),
('curriculum-v2-python-strings-hard-02','python','strings','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print("Warrior"[1:4] + "!")','["arr!", "No output", "An error occurs", "arr!!"]'::jsonb,0,'Text positions start at zero. Length counts all characters. A slice/substring selects part of the text; Python/Java stop before position 4, while C# takes 3 characters from position 1.',true,6),
('curriculum-v2-python-strings-hard-03','python','strings','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print("Journey"[1:4] + "!")','["No output", "our!", "An error occurs", "our!!"]'::jsonb,1,'Text positions start at zero. Length counts all characters. A slice/substring selects part of the text; Python/Java stop before position 4, while C# takes 3 characters from position 1.',true,6),
('curriculum-v2-python-strings-hard-04','python','strings','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','print("Crystal"[1:4] + "!")','["An error occurs", "No output", "rys!", "rys!!"]'::jsonb,2,'Text positions start at zero. Length counts all characters. A slice/substring selects part of the text; Python/Java stop before position 4, while C# takes 3 characters from position 1.',true,6),
('curriculum-v2-python-conditionals-hard-01','python','conditionals','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 3
keys = 0
if coins >= 5:
    if keys >= 1:
        print("Open")
    else:
        print("Need key")
else:
    print("Need coins")','["No output", "Need coins", "An error occurs", "Need coins!"]'::jsonb,1,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-python-conditionals-hard-02','python','conditionals','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 4
keys = 1
if coins >= 5:
    if keys >= 1:
        print("Open")
    else:
        print("Need key")
else:
    print("Need coins")','["An error occurs", "No output", "Need coins", "Need coins!"]'::jsonb,2,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-python-conditionals-hard-03','python','conditionals','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 5
keys = 0
if coins >= 5:
    if keys >= 1:
        print("Open")
    else:
        print("Need key")
else:
    print("Need coins")','["Need key!", "No output", "An error occurs", "Need key"]'::jsonb,3,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-python-conditionals-hard-04','python','conditionals','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','coins = 6
keys = 1
if coins >= 5:
    if keys >= 1:
        print("Open")
    else:
        print("Need key")
else:
    print("Need coins")','["Open", "No output", "An error occurs", "Open!"]'::jsonb,0,'Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.',true,8),
('curriculum-v2-python-loops-hard-01','python','loops','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','total = 0
for i in range(2):
    total = total + i * (i + 1)
print(total)','["1", "3", "2", "5"]'::jsonb,2,'The counter starts at 0 and stops before 2. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-python-loops-hard-02','python','loops','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','total = 0
for i in range(3):
    total = total + i * (i + 1)
print(total)','["11", "9", "7", "8"]'::jsonb,3,'The counter starts at 0 and stops before 3. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-python-loops-hard-03','python','loops','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','total = 0
for i in range(4):
    total = total + i * (i + 1)
print(total)','["20", "21", "19", "23"]'::jsonb,0,'The counter starts at 0 and stops before 4. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-python-loops-hard-04','python','loops','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','total = 0
for i in range(5):
    total = total + i * (i + 1)
print(total)','["41", "40", "39", "43"]'::jsonb,1,'The counter starts at 0 and stops before 5. Keep the updated total between repeats. A print after the loop runs only once.',true,9),
('curriculum-v2-python-arrays-hard-01','python','arrays','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','potions = [3, 6, 8]
total = 0
for i in range(len(potions)):
    if potions[i] > 6:
        total = total + potions[i]
print(total)','["11", "9", "7", "8"]'::jsonb,3,'Shelf positions start at 0. Read or change the value at that position. In the loop, add only values that pass the > 6 test.',true,10),
('curriculum-v2-python-arrays-hard-02','python','arrays','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','potions = [4, 7, 9]
total = 0
for i in range(len(potions)):
    if potions[i] > 6:
        total = total + potions[i]
print(total)','["16", "17", "15", "19"]'::jsonb,0,'Shelf positions start at 0. Read or change the value at that position. In the loop, add only values that pass the > 6 test.',true,10),
('curriculum-v2-python-arrays-hard-03','python','arrays','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','potions = [5, 8, 10]
total = 0
for i in range(len(potions)):
    if potions[i] > 6:
        total = total + potions[i]
print(total)','["19", "18", "17", "21"]'::jsonb,1,'Shelf positions start at 0. Read or change the value at that position. In the loop, add only values that pass the > 6 test.',true,10),
('curriculum-v2-python-arrays-hard-04','python','arrays','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','potions = [6, 9, 11]
total = 0
for i in range(len(potions)):
    if potions[i] > 6:
        total = total + potions[i]
print(total)','["19", "21", "20", "23"]'::jsonb,2,'Shelf positions start at 0. Read or change the value at that position. In the loop, add only values that pass the > 6 test.',true,10),
('curriculum-v2-python-methods-hard-01','python','methods','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','def boost(hp):
    return hp * 2 + 1
print(boost(boost(3)))','["15", "16", "14", "18"]'::jsonb,0,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-python-methods-hard-02','python','methods','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','def boost(hp):
    return hp * 2 + 1
print(boost(boost(4)))','["20", "19", "18", "22"]'::jsonb,1,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-python-methods-hard-03','python','methods','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','def boost(hp):
    return hp * 2 + 1
print(boost(boost(5)))','["22", "24", "23", "26"]'::jsonb,2,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-python-methods-hard-04','python','methods','hard',16,null,'predict_output','What messages or numbers will this code show? Read from top to bottom.','def boost(hp):
    return hp * 2 + 1
print(boost(boost(6)))','["30", "28", "26", "27"]'::jsonb,3,'The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.',true,11),
('curriculum-v2-java-objects-hard-01','java','objects','hard',16,null,'multiple_choice','What is a class used for?','','["A comparison operator", "A loop counter", "A printed message", "A blueprint for creating objects"]'::jsonb,3,'A blueprint for creating objects. Follow the stated rule one step at a time.',true,12),
('curriculum-v2-java-objects-hard-02','java','objects','hard',16,null,'multiple_choice','Two objects are made from the same class. Must their stored values always match?','','["No; each object can hold its own values", "Yes; all values are shared automatically", "Only if the class has a name", "Objects cannot store values"]'::jsonb,0,'No; each object can hold its own values. Follow the stated rule one step at a time.',true,12),
('curriculum-v2-java-objects-hard-03','java','objects','hard',16,null,'multiple_choice','What does an object method describe?','','["Only the filename", "An action the object can perform", "The number of array indexes", "A database password"]'::jsonb,1,'An action the object can perform. Follow the stated rule one step at a time.',true,12),
('curriculum-v2-java-objects-hard-04','java','objects','hard',16,null,'multiple_choice','What is an object instance?','','["A loop that never ends", "The class definition itself", "One particular object created from a class", "A text quotation mark"]'::jsonb,2,'One particular object created from a class. Follow the stated rule one step at a time.',true,12),
('curriculum-v2-java-collections-hard-01','java','collections','hard',16,null,'multiple_choice','Which collection rule removes the last item added first?','','["Stack: last in, first out", "Queue: first in, first out", "Alphabetical sorting", "Random selection"]'::jsonb,0,'Stack: last in, first out. Follow the stated rule one step at a time.',true,13),
('curriculum-v2-java-collections-hard-02','java','collections','hard',16,null,'multiple_choice','A queue receives A, then B, then C. Which item leaves first?','','["B", "A", "C", "All three at once"]'::jsonb,1,'A. Follow the stated rule one step at a time.',true,13),
('curriculum-v2-java-collections-hard-03','java','collections','hard',16,null,'multiple_choice','Why use a dictionary/map for player scores?','','["To repeat every statement forever", "To require numeric indexes starting at zero", "To look up a score using a player key", "To remove every duplicate score"]'::jsonb,2,'To look up a score using a player key. Follow the stated rule one step at a time.',true,13),
('curriculum-v2-java-collections-hard-04','java','collections','hard',16,null,'multiple_choice','What is a set useful for?','','["Storing only text", "Keeping a separate copy of every duplicate", "Always removing the newest value", "Keeping distinct values without duplicates"]'::jsonb,3,'Keeping distinct values without duplicates. Follow the stated rule one step at a time.',true,13),
('curriculum-v2-java-exceptions-hard-01','java','exceptions','hard',16,null,'multiple_choice','What is a try/catch block (try/except in Python) used for?','','["Repeating a loop", "Handling an error that happens while code runs", "Changing a value into a string", "Sorting a collection"]'::jsonb,1,'Handling an error that happens while code runs. Follow the stated rule one step at a time.',true,14),
('curriculum-v2-java-exceptions-hard-02','java','exceptions','hard',16,null,'multiple_choice','An error occurs inside try and a matching handler exists. What happens next?','','["The error becomes a correct answer", "The rest of try always runs first", "The matching catch/except handler runs", "The program restarts automatically"]'::jsonb,2,'The matching catch/except handler runs. Follow the stated rule one step at a time.',true,14),
('curriculum-v2-java-exceptions-hard-03','java','exceptions','hard',16,null,'multiple_choice','When is finally useful?','','["For making every test true", "For declaring an array index", "For skipping all cleanup", "For cleanup after try and its handler, such as closing a file"]'::jsonb,3,'For cleanup after try and its handler, such as closing a file. Follow the stated rule one step at a time.',true,14),
('curriculum-v2-java-exceptions-hard-04','java','exceptions','hard',16,null,'multiple_choice','What does throwing an exception (raising in Python) do?','','["Signals an error for a matching handler", "Repeats the last print", "Returns a normal success value", "Deletes the source code"]'::jsonb,0,'Signals an error for a matching handler. Follow the stated rule one step at a time.',true,14),
('curriculum-v2-java-algorithms-hard-01','java','algorithms','hard',16,null,'multiple_choice','What must be true before ordinary binary search is used?','','["The list must contain text only", "The values must all be equal", "The searched values must be sorted", "The list must be empty"]'::jsonb,2,'The searched values must be sorted. Follow the stated rule one step at a time.',true,15),
('curriculum-v2-java-algorithms-hard-02','java','algorithms','hard',16,null,'multiple_choice','Why does binary search need fewer checks than checking every item in a large sorted list?','','["It changes the target value", "It checks every item twice", "It always guesses the correct index", "Each comparison discards about half the remaining range"]'::jsonb,3,'Each comparison discards about half the remaining range. Follow the stated rule one step at a time.',true,15),
('curriculum-v2-java-algorithms-hard-03','java','algorithms','hard',16,null,'multiple_choice','What does linear search do?','','["Checks items one at a time until a match or the end", "Requires a recursive call for every item", "Always halves the search range", "Only checks the middle item"]'::jsonb,0,'Checks items one at a time until a match or the end. Follow the stated rule one step at a time.',true,15),
('curriculum-v2-java-algorithms-hard-04','java','algorithms','hard',16,null,'multiple_choice','What is the purpose of a sorting algorithm?','','["Remove all values", "Arrange values in a defined order", "Guarantee every value is different", "Convert numbers into objects"]'::jsonb,1,'Arrange values in a defined order. Follow the stated rule one step at a time.',true,15),
('curriculum-v2-java-recursion-hard-01','java','recursion','hard',18,null,'multiple_choice','Why does a recursive function need a base case?','','["To print every return twice", "To make the loop longer", "To share every variable globally", "To stop making more calls for a stopping input"]'::jsonb,3,'To stop making more calls for a stopping input. Follow the stated rule one step at a time.',true,16),
('curriculum-v2-java-recursion-hard-02','java','recursion','hard',18,null,'multiple_choice','A recursive countdown receives n. Which next input moves toward a base case of n <= 0?','','["n - 1", "n + 1", "n + 2", "The same n forever"]'::jsonb,0,'n - 1. Follow the stated rule one step at a time.',true,16),
('curriculum-v2-java-recursion-hard-03','java','recursion','hard',18,null,'multiple_choice','A recursive sum returns 0 for n = 0; otherwise it returns n + sum(n - 1). What is sum(3)?','','["3", "6", "9", "0"]'::jsonb,1,'6. Follow the stated rule one step at a time.',true,16),
('curriculum-v2-java-recursion-hard-04','java','recursion','hard',18,null,'multiple_choice','A recursive product returns 1 for n <= 1; otherwise it returns n * product(n - 1). What is product(4)?','','["10", "16", "24", "4"]'::jsonb,2,'24. Follow the stated rule one step at a time.',true,16),
('curriculum-v2-csharp-objects-hard-01','csharp','objects','hard',16,null,'multiple_choice','What is a class used for?','','["A comparison operator", "A loop counter", "A printed message", "A blueprint for creating objects"]'::jsonb,3,'A blueprint for creating objects. Follow the stated rule one step at a time.',true,12),
('curriculum-v2-csharp-objects-hard-02','csharp','objects','hard',16,null,'multiple_choice','Two objects are made from the same class. Must their stored values always match?','','["No; each object can hold its own values", "Yes; all values are shared automatically", "Only if the class has a name", "Objects cannot store values"]'::jsonb,0,'No; each object can hold its own values. Follow the stated rule one step at a time.',true,12),
('curriculum-v2-csharp-objects-hard-03','csharp','objects','hard',16,null,'multiple_choice','What does an object method describe?','','["Only the filename", "An action the object can perform", "The number of array indexes", "A database password"]'::jsonb,1,'An action the object can perform. Follow the stated rule one step at a time.',true,12),
('curriculum-v2-csharp-objects-hard-04','csharp','objects','hard',16,null,'multiple_choice','What is an object instance?','','["A loop that never ends", "The class definition itself", "One particular object created from a class", "A text quotation mark"]'::jsonb,2,'One particular object created from a class. Follow the stated rule one step at a time.',true,12),
('curriculum-v2-csharp-collections-hard-01','csharp','collections','hard',16,null,'multiple_choice','Which collection rule removes the last item added first?','','["Stack: last in, first out", "Queue: first in, first out", "Alphabetical sorting", "Random selection"]'::jsonb,0,'Stack: last in, first out. Follow the stated rule one step at a time.',true,13),
('curriculum-v2-csharp-collections-hard-02','csharp','collections','hard',16,null,'multiple_choice','A queue receives A, then B, then C. Which item leaves first?','','["B", "A", "C", "All three at once"]'::jsonb,1,'A. Follow the stated rule one step at a time.',true,13),
('curriculum-v2-csharp-collections-hard-03','csharp','collections','hard',16,null,'multiple_choice','Why use a dictionary/map for player scores?','','["To repeat every statement forever", "To require numeric indexes starting at zero", "To look up a score using a player key", "To remove every duplicate score"]'::jsonb,2,'To look up a score using a player key. Follow the stated rule one step at a time.',true,13),
('curriculum-v2-csharp-collections-hard-04','csharp','collections','hard',16,null,'multiple_choice','What is a set useful for?','','["Storing only text", "Keeping a separate copy of every duplicate", "Always removing the newest value", "Keeping distinct values without duplicates"]'::jsonb,3,'Keeping distinct values without duplicates. Follow the stated rule one step at a time.',true,13),
('curriculum-v2-csharp-exceptions-hard-01','csharp','exceptions','hard',16,null,'multiple_choice','What is a try/catch block (try/except in Python) used for?','','["Repeating a loop", "Handling an error that happens while code runs", "Changing a value into a string", "Sorting a collection"]'::jsonb,1,'Handling an error that happens while code runs. Follow the stated rule one step at a time.',true,14),
('curriculum-v2-csharp-exceptions-hard-02','csharp','exceptions','hard',16,null,'multiple_choice','An error occurs inside try and a matching handler exists. What happens next?','','["The error becomes a correct answer", "The rest of try always runs first", "The matching catch/except handler runs", "The program restarts automatically"]'::jsonb,2,'The matching catch/except handler runs. Follow the stated rule one step at a time.',true,14),
('curriculum-v2-csharp-exceptions-hard-03','csharp','exceptions','hard',16,null,'multiple_choice','When is finally useful?','','["For making every test true", "For declaring an array index", "For skipping all cleanup", "For cleanup after try and its handler, such as closing a file"]'::jsonb,3,'For cleanup after try and its handler, such as closing a file. Follow the stated rule one step at a time.',true,14),
('curriculum-v2-csharp-exceptions-hard-04','csharp','exceptions','hard',16,null,'multiple_choice','What does throwing an exception (raising in Python) do?','','["Signals an error for a matching handler", "Repeats the last print", "Returns a normal success value", "Deletes the source code"]'::jsonb,0,'Signals an error for a matching handler. Follow the stated rule one step at a time.',true,14),
('curriculum-v2-csharp-algorithms-hard-01','csharp','algorithms','hard',16,null,'multiple_choice','What must be true before ordinary binary search is used?','','["The list must contain text only", "The values must all be equal", "The searched values must be sorted", "The list must be empty"]'::jsonb,2,'The searched values must be sorted. Follow the stated rule one step at a time.',true,15),
('curriculum-v2-csharp-algorithms-hard-02','csharp','algorithms','hard',16,null,'multiple_choice','Why does binary search need fewer checks than checking every item in a large sorted list?','','["It changes the target value", "It checks every item twice", "It always guesses the correct index", "Each comparison discards about half the remaining range"]'::jsonb,3,'Each comparison discards about half the remaining range. Follow the stated rule one step at a time.',true,15),
('curriculum-v2-csharp-algorithms-hard-03','csharp','algorithms','hard',16,null,'multiple_choice','What does linear search do?','','["Checks items one at a time until a match or the end", "Requires a recursive call for every item", "Always halves the search range", "Only checks the middle item"]'::jsonb,0,'Checks items one at a time until a match or the end. Follow the stated rule one step at a time.',true,15),
('curriculum-v2-csharp-algorithms-hard-04','csharp','algorithms','hard',16,null,'multiple_choice','What is the purpose of a sorting algorithm?','','["Remove all values", "Arrange values in a defined order", "Guarantee every value is different", "Convert numbers into objects"]'::jsonb,1,'Arrange values in a defined order. Follow the stated rule one step at a time.',true,15),
('curriculum-v2-csharp-recursion-hard-01','csharp','recursion','hard',18,null,'multiple_choice','Why does a recursive function need a base case?','','["To print every return twice", "To make the loop longer", "To share every variable globally", "To stop making more calls for a stopping input"]'::jsonb,3,'To stop making more calls for a stopping input. Follow the stated rule one step at a time.',true,16),
('curriculum-v2-csharp-recursion-hard-02','csharp','recursion','hard',18,null,'multiple_choice','A recursive countdown receives n. Which next input moves toward a base case of n <= 0?','','["n - 1", "n + 1", "n + 2", "The same n forever"]'::jsonb,0,'n - 1. Follow the stated rule one step at a time.',true,16),
('curriculum-v2-csharp-recursion-hard-03','csharp','recursion','hard',18,null,'multiple_choice','A recursive sum returns 0 for n = 0; otherwise it returns n + sum(n - 1). What is sum(3)?','','["3", "6", "9", "0"]'::jsonb,1,'6. Follow the stated rule one step at a time.',true,16),
('curriculum-v2-csharp-recursion-hard-04','csharp','recursion','hard',18,null,'multiple_choice','A recursive product returns 1 for n <= 1; otherwise it returns n * product(n - 1). What is product(4)?','','["10", "16", "24", "4"]'::jsonb,2,'24. Follow the stated rule one step at a time.',true,16),
('curriculum-v2-python-objects-hard-01','python','objects','hard',16,null,'multiple_choice','What is a class used for?','','["A comparison operator", "A loop counter", "A printed message", "A blueprint for creating objects"]'::jsonb,3,'A blueprint for creating objects. Follow the stated rule one step at a time.',true,12),
('curriculum-v2-python-objects-hard-02','python','objects','hard',16,null,'multiple_choice','Two objects are made from the same class. Must their stored values always match?','','["No; each object can hold its own values", "Yes; all values are shared automatically", "Only if the class has a name", "Objects cannot store values"]'::jsonb,0,'No; each object can hold its own values. Follow the stated rule one step at a time.',true,12),
('curriculum-v2-python-objects-hard-03','python','objects','hard',16,null,'multiple_choice','What does an object method describe?','','["Only the filename", "An action the object can perform", "The number of array indexes", "A database password"]'::jsonb,1,'An action the object can perform. Follow the stated rule one step at a time.',true,12),
('curriculum-v2-python-objects-hard-04','python','objects','hard',16,null,'multiple_choice','What is an object instance?','','["A loop that never ends", "The class definition itself", "One particular object created from a class", "A text quotation mark"]'::jsonb,2,'One particular object created from a class. Follow the stated rule one step at a time.',true,12),
('curriculum-v2-python-collections-hard-01','python','collections','hard',16,null,'multiple_choice','Which collection rule removes the last item added first?','','["Stack: last in, first out", "Queue: first in, first out", "Alphabetical sorting", "Random selection"]'::jsonb,0,'Stack: last in, first out. Follow the stated rule one step at a time.',true,13),
('curriculum-v2-python-collections-hard-02','python','collections','hard',16,null,'multiple_choice','A queue receives A, then B, then C. Which item leaves first?','','["B", "A", "C", "All three at once"]'::jsonb,1,'A. Follow the stated rule one step at a time.',true,13),
('curriculum-v2-python-collections-hard-03','python','collections','hard',16,null,'multiple_choice','Why use a dictionary/map for player scores?','','["To repeat every statement forever", "To require numeric indexes starting at zero", "To look up a score using a player key", "To remove every duplicate score"]'::jsonb,2,'To look up a score using a player key. Follow the stated rule one step at a time.',true,13),
('curriculum-v2-python-collections-hard-04','python','collections','hard',16,null,'multiple_choice','What is a set useful for?','','["Storing only text", "Keeping a separate copy of every duplicate", "Always removing the newest value", "Keeping distinct values without duplicates"]'::jsonb,3,'Keeping distinct values without duplicates. Follow the stated rule one step at a time.',true,13),
('curriculum-v2-python-exceptions-hard-01','python','exceptions','hard',16,null,'multiple_choice','What is a try/catch block (try/except in Python) used for?','','["Repeating a loop", "Handling an error that happens while code runs", "Changing a value into a string", "Sorting a collection"]'::jsonb,1,'Handling an error that happens while code runs. Follow the stated rule one step at a time.',true,14),
('curriculum-v2-python-exceptions-hard-02','python','exceptions','hard',16,null,'multiple_choice','An error occurs inside try and a matching handler exists. What happens next?','','["The error becomes a correct answer", "The rest of try always runs first", "The matching catch/except handler runs", "The program restarts automatically"]'::jsonb,2,'The matching catch/except handler runs. Follow the stated rule one step at a time.',true,14),
('curriculum-v2-python-exceptions-hard-03','python','exceptions','hard',16,null,'multiple_choice','When is finally useful?','','["For making every test true", "For declaring an array index", "For skipping all cleanup", "For cleanup after try and its handler, such as closing a file"]'::jsonb,3,'For cleanup after try and its handler, such as closing a file. Follow the stated rule one step at a time.',true,14),
('curriculum-v2-python-exceptions-hard-04','python','exceptions','hard',16,null,'multiple_choice','What does throwing an exception (raising in Python) do?','','["Signals an error for a matching handler", "Repeats the last print", "Returns a normal success value", "Deletes the source code"]'::jsonb,0,'Signals an error for a matching handler. Follow the stated rule one step at a time.',true,14),
('curriculum-v2-python-algorithms-hard-01','python','algorithms','hard',16,null,'multiple_choice','What must be true before ordinary binary search is used?','','["The list must contain text only", "The values must all be equal", "The searched values must be sorted", "The list must be empty"]'::jsonb,2,'The searched values must be sorted. Follow the stated rule one step at a time.',true,15),
('curriculum-v2-python-algorithms-hard-02','python','algorithms','hard',16,null,'multiple_choice','Why does binary search need fewer checks than checking every item in a large sorted list?','','["It changes the target value", "It checks every item twice", "It always guesses the correct index", "Each comparison discards about half the remaining range"]'::jsonb,3,'Each comparison discards about half the remaining range. Follow the stated rule one step at a time.',true,15),
('curriculum-v2-python-algorithms-hard-03','python','algorithms','hard',16,null,'multiple_choice','What does linear search do?','','["Checks items one at a time until a match or the end", "Requires a recursive call for every item", "Always halves the search range", "Only checks the middle item"]'::jsonb,0,'Checks items one at a time until a match or the end. Follow the stated rule one step at a time.',true,15),
('curriculum-v2-python-algorithms-hard-04','python','algorithms','hard',16,null,'multiple_choice','What is the purpose of a sorting algorithm?','','["Remove all values", "Arrange values in a defined order", "Guarantee every value is different", "Convert numbers into objects"]'::jsonb,1,'Arrange values in a defined order. Follow the stated rule one step at a time.',true,15),
('curriculum-v2-python-recursion-hard-01','python','recursion','hard',18,null,'multiple_choice','Why does a recursive function need a base case?','','["To print every return twice", "To make the loop longer", "To share every variable globally", "To stop making more calls for a stopping input"]'::jsonb,3,'To stop making more calls for a stopping input. Follow the stated rule one step at a time.',true,16),
('curriculum-v2-python-recursion-hard-02','python','recursion','hard',18,null,'multiple_choice','A recursive countdown receives n. Which next input moves toward a base case of n <= 0?','','["n - 1", "n + 1", "n + 2", "The same n forever"]'::jsonb,0,'n - 1. Follow the stated rule one step at a time.',true,16),
('curriculum-v2-python-recursion-hard-03','python','recursion','hard',18,null,'multiple_choice','A recursive sum returns 0 for n = 0; otherwise it returns n + sum(n - 1). What is sum(3)?','','["3", "6", "9", "0"]'::jsonb,1,'6. Follow the stated rule one step at a time.',true,16),
('curriculum-v2-python-recursion-hard-04','python','recursion','hard',18,null,'multiple_choice','A recursive product returns 1 for n <= 1; otherwise it returns n * product(n - 1). What is product(4)?','','["10", "16", "24", "4"]'::jsonb,2,'24. Follow the stated rule one step at a time.',true,16)
on conflict (id) do nothing;
commit;
-- Browse in teaching order: select * from public.question_bank order by language,curriculum_order,min_level,id;
