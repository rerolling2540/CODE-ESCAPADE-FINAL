-- Run once in Supabase SQL Editor. Creates and seeds 90 beginner-to-advanced
-- practice questions (Java, C#, Python; 10 questions in each level band).
-- Level bands: 1-5 easy, 6-15 medium, 16+ hard.
create table if not exists public.question_bank (
    id text primary key,
    language text not null check (language in ('java', 'csharp', 'python')),
    difficulty text not null check (difficulty in ('easy', 'medium', 'hard')),
    min_level integer not null check (min_level >= 1),
    max_level integer,
    question_type text not null check (question_type in ('multiple_choice', 'fill_blank', 'missing_code', 'fix_code', 'predict_output')),
    prompt text not null,
    code_snippet text not null default '',
    choices jsonb not null check (jsonb_typeof(choices) = 'array' and jsonb_array_length(choices) between 2 and 4),
    correct_index integer not null check (correct_index >= 0),
    explanation text not null,
    published boolean not null default true,
    created_at timestamptz not null default now(),
    constraint question_answer_index_valid check (correct_index < jsonb_array_length(choices)),
    constraint question_level_band check (max_level is null or max_level >= min_level)
);

create index if not exists question_bank_lookup_idx
    on public.question_bank (language, min_level, max_level) where published;

alter table public.question_bank enable row level security;
drop policy if exists "Published questions are readable" on public.question_bank;
create policy "Published questions are readable" on public.question_bank
    for select to anon, authenticated using (published = true);
revoke insert, update, delete, truncate, references, trigger on public.question_bank from anon, authenticated;
grant select on public.question_bank to anon, authenticated;

-- Admin-only editor access. Run this block after the table exists, or run the
-- standalone question_bank_admin_policy.sql migration for an existing project.
grant insert, update, delete on public.question_bank to authenticated;
drop policy if exists "Admin can manage question bank" on public.question_bank;
create policy "Admin can manage question bank" on public.question_bank
    for all to authenticated
    using (lower(coalesce(auth.jwt() ->> 'email', '')) = 'adeventurerank60@gmail.com')
    with check (lower(coalesce(auth.jwt() ->> 'email', '')) = 'adeventurerank60@gmail.com');

-- Seed is repeatable: re-running updates existing IDs instead of duplicating rows.
-- Keep correct answers in this client-readable table only for a learning game;
-- a determined player can inspect any answer delivered to the game client.
insert into public.question_bank
    (id, language, difficulty, min_level, max_level, question_type, prompt, code_snippet, choices, correct_index, explanation)
values
    ('java-e01','java','easy',1,5,'predict_output','What does this print?','System.out.println(2 + 3);','["5","23","6","Compilation error"]',0,'The integer values are added.'),
    ('java-e02','java','easy',1,5,'fill_blank','Fill the blank with the whole-number type.','___ score = 12;','["int","String","boolean","double"]',0,'int stores whole-number values.'),
    ('java-e03','java','easy',1,5,'predict_output','What is printed?','String name = "Ada";\nSystem.out.println("Hi " + name);','["Hi Ada","Hi name","Ada Hi","Compilation error"]',0,'The + operator joins the string and name.'),
    ('java-e04','java','easy',1,5,'missing_code','Which operator checks whether age equals 18?','if (age ___ 18) { ... }','["==","=","!=","<="]',0,'== compares values; = assigns.'),
    ('java-e05','java','easy',1,5,'predict_output','What is printed?','int[] nums = {4, 8};\nSystem.out.println(nums[0]);','["4","8","0","Compilation error"]',0,'The first array element is at index 0.'),
    ('java-e06','java','easy',1,5,'missing_code','Choose the keyword that makes this method return a value.','static int doubleIt(int n) { ___ n * 2; }','["return","break","void","new"]',0,'return sends the computed value back to the caller.'),
    ('java-e07','java','easy',1,5,'predict_output','What characters are printed?','for (int i = 0; i < 3; i++) System.out.print(i);','["012","123","03","0123"]',0,'The loop visits i values 0, 1, and 2.'),
    ('java-e08','java','easy',1,5,'fix_code','Choose the correction for this missing semicolon.','int lives = 3','["int lives = 3;","int lives == 3;","int = lives 3;","int lives : 3;"]',0,'Java statements end with a semicolon.'),
    ('java-e09','java','easy',1,5,'predict_output','What is printed?','System.out.println(7 > 4);','["true","false","7","4"]',0,'7 is greater than 4, so the boolean result is true.'),
    ('java-e10','java','easy',1,5,'multiple_choice','Which type stores true or false?','','["boolean","int","String","char"]',0,'boolean is Java’s true/false type.'),
    ('java-m01','java','medium',6,15,'predict_output','What is the final value of sum?','int sum = 0;\nfor (int n : new int[]{2, 4, 6}) sum += n;\nSystem.out.println(sum);','["12","6","10","246"]',0,'The loop accumulates 2 + 4 + 6.'),
    ('java-m02','java','medium',6,15,'predict_output','What does this print?','String word = "code";\nSystem.out.println(word.substring(1, 3));','["od","co","ode","code"]',0,'substring includes the start index and excludes the end index.'),
    ('java-m03','java','medium',6,15,'missing_code','Fill the condition to visit indexes 0 through 2 safely.','for (int i = 0; i ___ values.length; i++)','["<","<=",">","=="]',0,'The last valid index is length - 1, so use < length.'),
    ('java-m04','java','medium',6,15,'predict_output','What is printed?','int x = 5;\nSystem.out.println(x++);\nSystem.out.println(x);','["5 then 6","6 then 6","5 then 5","6 then 7"]',0,'Post-increment yields the old value, then increments x.'),
    ('java-m05','java','medium',6,15,'fix_code','Which line safely checks whether text is the word "yes"?','if (text == "yes") { ... }','["if (text.equals(\"yes\")) { ... }","if (text = \"yes\") { ... }","if (text.compare(\"yes\")) { ... }","if (text != \"yes\") { ... }"]',0,'Use String.equals to compare Java string contents.'),
    ('java-m06','java','medium',6,15,'predict_output','What is printed?','int[] a = {3, 1, 4};\nSystem.out.println(a.length);','["3","4","2","1"]',0,'The array contains three elements.'),
    ('java-m07','java','medium',6,15,'fill_blank','Fill in the return type for this method.','___ add(int a, int b) { return a + b; }','["int","void","String","boolean"]',0,'The method returns the result of integer addition.'),
    ('java-m08','java','medium',6,15,'predict_output','What is printed?','int n = 9;\nif (n % 2 == 0) System.out.println("even"); else System.out.println("odd");','["odd","even","9","Nothing"]',0,'9 divided by 2 has remainder 1.'),
    ('java-m09','java','medium',6,15,'missing_code','Which keyword skips to the next loop iteration?','if (i == 2) ___;','["continue","break","return","throw"]',0,'continue skips the remaining body and advances the loop.'),
    ('java-m10','java','medium',6,15,'predict_output','What is printed?','int[] a = {1, 2};\na[1] = a[0] + 5;\nSystem.out.println(a[1]);','["6","5","3","2"]',0,'a[1] becomes 1 + 5.'),
    ('java-h01','java','hard',16,null,'predict_output','What value is returned by this recursion?','static int f(int n) { return n <= 1 ? 1 : n * f(n - 1); }\nSystem.out.println(f(4));','["24","16","10","4"]',0,'The recursive product is 4 × 3 × 2 × 1.'),
    ('java-h02','java','hard',16,null,'multiple_choice','What is the time complexity of binary search on a sorted array?','','["O(log n)","O(n)","O(n log n)","O(1) always"]',0,'Each step halves the remaining search interval.'),
    ('java-h03','java','hard',16,null,'predict_output','What is printed?','List<Integer> xs = new ArrayList<>(List.of(1, 2));\nxs.add(3);\nSystem.out.println(xs.size());','["3","2","4","Compilation error"]',0,'Adding one element increases the list size from 2 to 3.'),
    ('java-h04','java','hard',16,null,'fix_code','How should this code compare two objects by value?','if (a == b) { /* equal contents? */ }','["Use a.equals(b)","Use a = b","Use a.compare(b) == true","Use a !== b"]',0,'For most Java objects, equals checks logical equality.'),
    ('java-h05','java','hard',16,null,'predict_output','What is printed?','try { int x = 10 / 0; } catch (ArithmeticException e) { System.out.println("caught"); }','["caught","0","10","Program prints nothing"]',0,'Integer division by zero throws ArithmeticException, which is caught.'),
    ('java-h06','java','hard',16,null,'missing_code','Fill in the generic type parameter for a list of strings.','List<___> names = new ArrayList<>();','["String","int","void","boolean"]',0,'Java generics use reference types such as String.'),
    ('java-h07','java','hard',16,null,'predict_output','What is printed?','int x = 2;\nSystem.out.println(x * (x + 3));','["10","7","13","12"]',0,'2 × (2 + 3) equals 10.'),
    ('java-h08','java','hard',16,null,'multiple_choice','Which data structure is designed for last-in, first-out access?','','["Stack","Queue","HashMap","TreeSet"]',0,'A stack removes the most recently added item first.'),
    ('java-h09','java','hard',16,null,'predict_output','What is printed?','int[] a = {2, 4, 6};\nint total = 0;\nfor (int v : a) total += v / 2;\nSystem.out.println(total);','["6","12","5","3"]',0,'Integer divisions give 1 + 2 + 3 = 6.'),
    ('java-h10','java','hard',16,null,'multiple_choice','What must a recursive method have to eventually stop calling itself?','','["A base case","A static field","A for loop","A constructor"]',0,'A base case stops recursion.'),
    ('csharp-e01','csharp','easy',1,5,'predict_output','What does this print?','Console.WriteLine(2 + 3);','["5","23","6","Compiler error"]',0,'The integer values are added.'),
    ('csharp-e02','csharp','easy',1,5,'fill_blank','Fill the blank with the whole-number type.','___ score = 12;','["int","string","bool","double"]',0,'int stores whole-number values.'),
    ('csharp-e03','csharp','easy',1,5,'predict_output','What is printed?','string name = "Ada";\nConsole.WriteLine("Hi " + name);','["Hi Ada","Hi name","Ada Hi","Compiler error"]',0,'The + operator joins the string and name.'),
    ('csharp-e04','csharp','easy',1,5,'missing_code','Which operator checks whether age equals 18?','if (age ___ 18) { ... }','["==","=","!=","<="]',0,'== compares values; = assigns.'),
    ('csharp-e05','csharp','easy',1,5,'predict_output','What is printed?','int[] nums = { 4, 8 };\nConsole.WriteLine(nums[0]);','["4","8","0","Compiler error"]',0,'The first array element is at index 0.'),
    ('csharp-e06','csharp','easy',1,5,'missing_code','Choose the keyword that sends this result back.','static int DoubleIt(int n) { ___ n * 2; }','["return","break","void","new"]',0,'return sends a value to the caller.'),
    ('csharp-e07','csharp','easy',1,5,'predict_output','What characters are printed?','for (int i = 0; i < 3; i++) Console.Write(i);','["012","123","03","0123"]',0,'The loop visits 0, 1, and 2.'),
    ('csharp-e08','csharp','easy',1,5,'fix_code','Choose the correction for this missing semicolon.','int lives = 3','["int lives = 3;","int lives == 3;","int = lives 3;","int lives : 3;"]',0,'C# statements end with a semicolon.'),
    ('csharp-e09','csharp','easy',1,5,'predict_output','What is printed?','Console.WriteLine(7 > 4);','["True","False","7","4"]',0,'7 is greater than 4, so the boolean result is true.'),
    ('csharp-e10','csharp','easy',1,5,'multiple_choice','Which type stores true or false?','','["bool","int","string","char"]',0,'bool is C#’s true/false type.'),
    ('csharp-m01','csharp','medium',6,15,'predict_output','What is the final value of sum?','int sum = 0;\nforeach (int n in new[] { 2, 4, 6 }) sum += n;\nConsole.WriteLine(sum);','["12","6","10","246"]',0,'The loop accumulates 2 + 4 + 6.'),
    ('csharp-m02','csharp','medium',6,15,'predict_output','What does this print?','string word = "code";\nConsole.WriteLine(word.Substring(1, 2));','["od","co","ode","code"]',0,'Substring starts at index 1 and takes 2 characters.'),
    ('csharp-m03','csharp','medium',6,15,'missing_code','Fill the condition to visit indexes safely.','for (int i = 0; i ___ values.Length; i++)','["<","<=",">","=="]',0,'Use < length so the final index is length - 1.'),
    ('csharp-m04','csharp','medium',6,15,'predict_output','What is printed?','int x = 5;\nConsole.WriteLine(x++);\nConsole.WriteLine(x);','["5 then 6","6 then 6","5 then 5","6 then 7"]',0,'Post-increment yields old x, then increments it.'),
    ('csharp-m05','csharp','medium',6,15,'fix_code','Which comparison checks string contents?','if (text == "yes") { ... }','["if (text == \"yes\") { ... }","if (text = \"yes\") { ... }","if (text.compare(\"yes\")) { ... }","if (text !== \"yes\") { ... }"]',0,'C# string == compares string values.'),
    ('csharp-m06','csharp','medium',6,15,'predict_output','What is printed?','int[] a = { 3, 1, 4 };\nConsole.WriteLine(a.Length);','["3","4","2","1"]',0,'The array contains three elements.'),
    ('csharp-m07','csharp','medium',6,15,'fill_blank','Fill in the return type for this method.','___ Add(int a, int b) { return a + b; }','["int","void","string","bool"]',0,'The method returns an integer sum.'),
    ('csharp-m08','csharp','medium',6,15,'predict_output','What is printed?','int n = 9;\nConsole.WriteLine(n % 2 == 0 ? "even" : "odd");','["odd","even","9","Nothing"]',0,'9 has remainder 1 when divided by 2.'),
    ('csharp-m09','csharp','medium',6,15,'missing_code','Which keyword skips to the next loop iteration?','if (i == 2) ___;','["continue","break","return","throw"]',0,'continue advances to the next iteration.'),
    ('csharp-m10','csharp','medium',6,15,'predict_output','What is printed?','int[] a = { 1, 2 };\na[1] = a[0] + 5;\nConsole.WriteLine(a[1]);','["6","5","3","2"]',0,'a[1] becomes 1 + 5.'),
    ('csharp-h01','csharp','hard',16,null,'predict_output','What value is returned by this recursion?','int F(int n) => n <= 1 ? 1 : n * F(n - 1);\nConsole.WriteLine(F(4));','["24","16","10","4"]',0,'The recursive product is 4 × 3 × 2 × 1.'),
    ('csharp-h02','csharp','hard',16,null,'multiple_choice','What is binary search time complexity on a sorted array?','','["O(log n)","O(n)","O(n log n)","O(1) always"]',0,'Each step halves the search interval.'),
    ('csharp-h03','csharp','hard',16,null,'predict_output','What is printed?','var xs = new List<int> { 1, 2 };\nxs.Add(3);\nConsole.WriteLine(xs.Count);','["3","2","4","Compiler error"]',0,'Adding one element changes the count from 2 to 3.'),
    ('csharp-h04','csharp','hard',16,null,'fix_code','How should a custom class compare object values?','if (a == b) { /* equal contents? */ }','["Override Equals or compare a value property","Use a = b","Use a.compare(b) == true","Use a !== b"]',0,'For custom reference types, define value equality explicitly when needed.'),
    ('csharp-h05','csharp','hard',16,null,'predict_output','What is printed?','try { int x = 10 / int.Parse("0"); } catch (DivideByZeroException) { Console.WriteLine("caught"); }','["caught","0","10","Nothing"]',0,'Integer division by zero throws DivideByZeroException.'),
    ('csharp-h06','csharp','hard',16,null,'missing_code','Fill in the generic type for a list of strings.','List<___> names = new List<string>();','["string","int","void","bool"]',0,'The collection holds string values.'),
    ('csharp-h07','csharp','hard',16,null,'predict_output','What is printed?','int x = 2;\nConsole.WriteLine(x * (x + 3));','["10","7","13","12"]',0,'2 × (2 + 3) equals 10.'),
    ('csharp-h08','csharp','hard',16,null,'multiple_choice','Which structure is last-in, first-out?','','["Stack","Queue","Dictionary","HashSet"]',0,'A stack removes the newest item first.'),
    ('csharp-h09','csharp','hard',16,null,'predict_output','What is printed?','int[] a = { 2, 4, 6 };\nint total = 0;\nforeach (int v in a) total += v / 2;\nConsole.WriteLine(total);','["6","12","5","3"]',0,'Integer division gives 1 + 2 + 3.'),
    ('csharp-h10','csharp','hard',16,null,'multiple_choice','What prevents recursion from continuing forever?','','["A base case","A static field","A for loop","A constructor"]',0,'A base case terminates recursive calls.'),
    ('python-e01','python','easy',1,5,'predict_output','What does this print?','print(2 + 3)','["5","23","6","SyntaxError"]',0,'The integer values are added.'),
    ('python-e02','python','easy',1,5,'fill_blank','Fill the blank with a Python whole-number value.','score = ___','["12","\"twelve\"","True","12.5"]',0,'12 is an integer literal; Python variables do not need a declared type.'),
    ('python-e03','python','easy',1,5,'predict_output','What is printed?','name = "Ada"\nprint("Hi " + name)','["Hi Ada","Hi name","Ada Hi","TypeError"]',0,'The + operator joins the two strings.'),
    ('python-e04','python','easy',1,5,'missing_code','Which operator checks whether age equals 18?','if age ___ 18:','["==","=","!=","<="]',0,'== compares values; = assigns.'),
    ('python-e05','python','easy',1,5,'predict_output','What is printed?','nums = [4, 8]\nprint(nums[0])','["4","8","0","IndexError"]',0,'The first list element uses index 0.'),
    ('python-e06','python','easy',1,5,'missing_code','Which keyword returns the result from this function?','def double_it(n):\n    ___ n * 2','["return","break","yield","class"]',0,'return sends the computed value to the caller.'),
    ('python-e07','python','easy',1,5,'predict_output','What numbers are printed?','for i in range(3):\n    print(i, end="")','["012","123","03","0123"]',0,'range(3) produces 0, 1, and 2.'),
    ('python-e08','python','easy',1,5,'fix_code','Choose the correction for this assignment.','lives == 3','["lives = 3","lives :=: 3","int lives = 3;","lives : 3"]',0,'A single = assigns a value.'),
    ('python-e09','python','easy',1,5,'predict_output','What is printed?','print(7 > 4)','["True","False","7","4"]',0,'7 is greater than 4.'),
    ('python-e10','python','easy',1,5,'multiple_choice','Which literal represents true in Python?','','["True","true","1.0","yes"]',0,'Python spells the boolean literal True with a capital T.'),
    ('python-m01','python','medium',6,15,'predict_output','What is the final value of total?','total = 0\nfor n in [2, 4, 6]:\n    total += n\nprint(total)','["12","6","10","246"]',0,'The loop adds 2 + 4 + 6.'),
    ('python-m02','python','medium',6,15,'predict_output','What does this print?','word = "code"\nprint(word[1:3])','["od","co","ode","code"]',0,'The slice includes index 1 and excludes index 3.'),
    ('python-m03','python','medium',6,15,'missing_code','Fill the range bound to visit every valid index.','for i in range(___):\n    print(values[i])','["len(values)","len(values) - 1","len(values) + 1","1"]',0,'range(len(values)) generates indexes from zero to length minus one.'),
    ('python-m04','python','medium',6,15,'predict_output','What is printed?','x = 5\nprint(x)\nx += 1\nprint(x)','["5 then 6","6 then 6","5 then 5","6 then 7"]',0,'x starts at 5, then += 1 changes it to 6.'),
    ('python-m05','python','medium',6,15,'fix_code','Choose the equality check for the word yes.','if text = "yes":','["if text == \"yes\":","if text === \"yes\":","if text.equals(\"yes\"): ","if text := \"yes\":"]',0,'Python uses == for equality.'),
    ('python-m06','python','medium',6,15,'predict_output','What is printed?','a = [3, 1, 4]\nprint(len(a))','["3","4","2","1"]',0,'The list contains three elements.'),
    ('python-m07','python','medium',6,15,'fill_blank','Fill in the function keyword.','___ add(a, b):\n    return a + b','["def","func","method","return"]',0,'def declares a Python function.'),
    ('python-m08','python','medium',6,15,'predict_output','What is printed?','n = 9\nprint("even" if n % 2 == 0 else "odd")','["odd","even","9","Nothing"]',0,'9 has remainder 1 after division by 2.'),
    ('python-m09','python','medium',6,15,'missing_code','Which keyword skips to the next loop iteration?','if i == 2:\n    ___','["continue","break","return","pass"]',0,'continue skips the rest of the current iteration.'),
    ('python-m10','python','medium',6,15,'predict_output','What is printed?','a = [1, 2]\na[1] = a[0] + 5\nprint(a[1])','["6","5","3","2"]',0,'The second element becomes 1 + 5.'),
    ('python-h01','python','hard',16,null,'predict_output','What value does this recursion return?','def f(n):\n    return 1 if n <= 1 else n * f(n - 1)\nprint(f(4))','["24","16","10","4"]',0,'The recursive product is 4 × 3 × 2 × 1.'),
    ('python-h02','python','hard',16,null,'multiple_choice','What is binary search time complexity on a sorted list?','','["O(log n)","O(n)","O(n log n)","O(1) always"]',0,'Each comparison halves the search interval.'),
    ('python-h03','python','hard',16,null,'predict_output','What is printed?','xs = [1, 2]\nxs.append(3)\nprint(len(xs))','["3","2","4","IndexError"]',0,'Appending one element changes the length from 2 to 3.'),
    ('python-h04','python','hard',16,null,'fix_code','Which method defines value equality for a custom class?','class Token: pass','["Implement __eq__","Implement __add__","Implement __len__ only","Use ==="]',0,'Python custom value equality is commonly implemented with __eq__.'),
    ('python-h05','python','hard',16,null,'predict_output','What is printed?','try:\n    x = 10 // 0\nexcept ZeroDivisionError:\n    print("caught")','["caught","0","10","Nothing"]',0,'Integer division by zero raises ZeroDivisionError and is caught.'),
    ('python-h06','python','hard',16,null,'missing_code','Fill in the expression to keep only even numbers.','evens = [n for n in nums if n ___ 2 == 0]','["%","//","/","*"]',0,'% gives the remainder; even values have remainder zero.'),
    ('python-h07','python','hard',16,null,'predict_output','What is printed?','x = 2\nprint(x * (x + 3))','["10","7","13","12"]',0,'2 multiplied by (2 + 3) is 10.'),
    ('python-h08','python','hard',16,null,'multiple_choice','Which standard collection is last-in, first-out when used with append and pop?','','["list","deque used as queue","set","dict"]',0,'A list append followed by pop uses LIFO behavior.'),
    ('python-h09','python','hard',16,null,'predict_output','What is printed?','a = [2, 4, 6]\ntotal = sum(v // 2 for v in a)\nprint(total)','["6","12","5","3"]',0,'The integer divisions sum to 1 + 2 + 3.'),
    ('python-h10','python','hard',16,null,'multiple_choice','What prevents recursion from continuing forever?','','["A base case","A global variable","A list","A print statement"]',0,'A base case stops recursive calls.')
on conflict (id) do update set
    language = excluded.language, difficulty = excluded.difficulty,
    min_level = excluded.min_level, max_level = excluded.max_level,
    question_type = excluded.question_type, prompt = excluded.prompt,
    code_snippet = excluded.code_snippet, choices = excluded.choices,
    correct_index = excluded.correct_index, explanation = excluded.explanation,
    published = true;
