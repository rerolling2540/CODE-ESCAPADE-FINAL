"""Build original question variations and a non-destructive Supabase migration.
Run with the pre-change published-bank snapshot as the first argument.
"""
import contextlib, io, json, pathlib, random, sys

ROOT=pathlib.Path(__file__).resolve().parents[3]
TOPICS=['output','syntax','types','variables','operators','strings','comparisons','conditionals','loops','arrays','methods','objects','collections','exceptions','algorithms','recursion']
LEVELS=[1,1,1,2,2,3,6,7,8,10,12,14,16,16,16,18]
BASE=dict(zip(TOPICS,LEVELS))
TIERS={'easy':TOPICS[:11],'medium':[t for t in TOPICS[:11] if t not in ('syntax','types')],'hard':['operators','strings','conditionals','loops','arrays','methods']}
old=json.loads(pathlib.Path(sys.argv[1]).read_text(encoding='utf-8-sig'))
rows=[]
def level(topic,difficulty):return max(BASE.get(topic,1),{'easy':1,'medium':6,'hard':16}[difficulty])
def literal(s):return json.dumps(s)
for lang in ['java','csharp','python']:
 py=lang=='python';cs=lang=='csharp'
 def emit(x):return 'print('+x+')' if py else ('Console.WriteLine(' if cs else 'System.out.println(')+x+');'
 def var(n,x):return f'{n} = {x}' if py else f'int {n} = {x};'
 def update(n,x):return f'{n} = {x}'+('' if py else ';')
 def loop(n,body):return f'for i in range({n}):\n'+''.join('    '+line+'\n' for line in body.splitlines()).rstrip() if py else f'for (int i = 0; i < {n}; i++) {{\n'+''.join('    '+line+'\n' for line in body.splitlines())+'}'
 for difficulty,topics in TIERS.items():
  tier={'easy':0,'medium':1,'hard':2}[difficulty]
  for topic in topics:
   for v in range(4):
    a=3+v;b=2+v;n=2+v;answer=None;kind='predict_output';reason=''
    if topic=='output':
     if tier==0:code=emit(literal(['Lantern ready','Bridge repaired','Quest accepted','Shield raised'][v]));answer=['Lantern ready','Bridge repaired','Quest accepted','Shield raised'][v]
     else:expression=f'{a} + {b}' if tier==1 else f'({a} + {b}) * 2';code=emit(expression)+'\n'+emit(literal('Ready'));answer=str((a+b)*(2 if tier==2 else 1))+'\nReady'
     reason='Read each print instruction from top to bottom. Calculate numbers inside the print instruction before showing them; each instruction starts a new line.'
    elif topic=='variables':
     code=var('coins',str(a));value=a
     if tier:code+='\n'+update('coins',f'coins + {b}');value+=b
     code+='\n'+emit('coins');answer=str(value);reason='coins is a named box. The = sign stores the number on its right. The last print shows the latest stored number.'
    elif topic=='operators':
     expr=f'{a} + {b}' if tier==0 else f'{a} * {b} - 1' if tier==1 else f'({a} + {b}) * 3 % 5';value=a+b if tier==0 else a*b-1 if tier==1 else (a+b)*3%5
     code=emit(expr);answer=str(value);reason='Work inside parentheses first, then multiplication and remainder (%), then addition or subtraction. Remainder is what is left after division.'
    elif topic=='strings':
     word=['Lantern','Warrior','Journey','Crystal'][v]
     if tier==0:code=emit(literal('Hi ')+ ' + '+literal(word));answer='Hi '+word
     elif tier==1:code=emit(f'len({literal(word)})' if py else literal(word)+('.Length' if cs else '.length()'));answer=str(len(word))
     else:part=literal(word)+('[1:4]' if py else '.Substring(1, 3)' if cs else '.substring(1, 4)');code=emit(part+' + '+literal('!'));answer=word[1:4]+'!'
     reason='Text positions start at zero. Length counts all characters. A slice/substring selects part of the text; Python/Java stop before position 4, while C# takes 3 characters from position 1.' if tier==2 else 'Joining text keeps its order and spaces. Length counts characters, including any spaces.'
    elif topic=='comparisons':
     expr=f'{a} > {b}' if v%2==0 else f'{b} >= {a}';value=a>b if v%2==0 else b>=a
     if tier:expr+=' and '+f'{a} < 10' if py else ' && '+f'{a} < 10';value=value and a<10
     code=emit(expr);answer=('True' if value else 'False') if py or cs else ('true' if value else 'false');reason='A comparison gives a yes/no value. > means greater than; >= includes equality. AND requires both tests to be true.'
    elif topic=='conditionals':
     value=a>=5;test='coins >= 5';code=var('coins',str(a))
     if tier:code+='\n'+var('keys',str(v%2));test+=' and keys >= 1' if py else ' && keys >= 1';value=value and v%2>=1
     if tier==2:
      code+='\n'+(f'if coins >= 5:\n    if keys >= 1:\n        print("Open")\n    else:\n        print("Need key")\nelse:\n    print("Need coins")' if py else 'if (coins >= 5) {\n    if (keys >= 1) {\n        '+emit('"Open"')+'\n    } else {\n        '+emit('"Need key"')+'\n    }\n} else {\n    '+emit('"Need coins"')+'\n}');answer='Need coins' if a<5 else 'Open' if v%2 else 'Need key'
     else:code+='\n'+(f'if {test}:\n    print("Open")\nelse:\n    print("Locked")' if py else 'if ('+test+') {\n    '+emit('"Open"')+'\n} else {\n    '+emit('"Locked"')+'\n}');answer='Open' if value else 'Locked'
     reason='Test the outer condition first. Follow only the chosen path. A second test inside that path runs only if you reach it.'
    elif topic=='loops':
     if tier==0:code=loop(n,emit('i'));answer='\n'.join(map(str,range(n)))
     else:code=var('total','0')+'\n'+loop(n,update('total','total + '+('2' if tier==1 else 'i * (i + 1)')))+'\n'+emit('total');answer=str(n*2 if tier==1 else sum(i*(i+1) for i in range(n)))
     reason='The counter starts at 0 and stops before '+str(n)+'. Keep the updated total between repeats. A print after the loop runs only once.'
    elif topic=='arrays':
     values=[a,a+3,a+5];code='potions = '+str(values) if py else 'int[] potions = {'+', '.join(map(str,values))+'};'
     if tier==0:index=v%3;code+='\n'+emit(f'potions[{index}]');answer=str(values[index])
     elif tier==1:code+='\n'+update('potions[1]',f'potions[0] + {b}')+'\n'+emit('potions[1]');answer=str(a+b)
     else:
      size='len(potions)' if py else 'potions.Length' if cs else 'potions.length';body=('if potions[i] > 6:\n    total = total + potions[i]' if py else 'if (potions[i] > 6) {\n    total = total + potions[i];\n}')
      code+='\n'+var('total','0')+'\n'+loop(size,body)+'\n'+emit('total');answer=str(sum(x for x in values if x>6))
     reason='Shelf positions start at 0. Read or change the value at that position. In the loop, add only values that pass the > 6 test.' if tier==2 else 'Index 0 is the first shelf space. Assignment changes the selected space; other spaces keep their values.'
    elif topic=='methods':
     name='Boost' if cs else 'boost';expr='hp + 2' if tier==0 else 'hp * 2 + 1';body='def boost(hp):\n    return '+expr if py else 'static int '+name+'(int hp) {\n    return '+expr+';\n}'
     call=f'{name}({a})' if tier<2 else f'{name}({name}({a}))';code=body+'\n'+emit(call);answer=str(a+2 if tier==0 else a*2+1 if tier==1 else (a*2+1)*2+1);reason='The input goes into hp. Work out the return expression and give its answer back. If one use is inside another, solve the inside one first.'
    elif topic=='syntax':
     kind='fill_blank';code=f'if {a} > {b}___\n    print("Ready {v+1}")' if py else f'int coins = {a}___';answer=':' if py else ';';reason='Python starts an indented block with a colon. Java/C# end this assignment statement with a semicolon.'
    elif topic=='types':
     kind='multiple_choice';code='';answer='int';reason='int is the type name for whole numbers, such as 3 or 12.'
    prompt='What messages or numbers will this code show? Read from top to bottom.' if kind=='predict_output' else 'Fill the blank with the punctuation that makes this code valid.' if kind=='fill_blank' else 'Which type name describes a whole number, such as '+str(a)+'?'
    if topic=='types':answers=[answer,'str' if py else 'string' if cs else 'String','bool' if py or cs else 'boolean','float' if py else 'double']
    elif topic=='syntax':answers=[answer,',' if py else ':',';' if py else '==','.' if py else ',']
    elif answer.lstrip('-').isdigit():answers=[answer,str(int(answer)+1),str(int(answer)-1),str(int(answer)+3)]
    elif answer in ('true','false','True','False'):answers=[answer,('False' if answer=='True' else 'True') if py or cs else ('false' if answer=='true' else 'true'),'No output','An error occurs']
    else:answers=[answer,'No output','An error occurs',answer+'!']
    identity=f'curriculum-v2-{lang}-{topic}-{difficulty}-{v+1:02}'
    # Each position is correct equally often in each four-variation family.
    correct_index=(v+TOPICS.index(topic)+tier)%4;answers[0],answers[correct_index]=answers[correct_index],answers[0]
    row=dict(id=identity,language=lang,topic=topic,difficulty=difficulty,min_level=level(topic,difficulty),max_level=None,question_type=kind,prompt=prompt,code_snippet=code,choices=answers,correct_index=correct_index,explanation=reason,published=True,curriculum_order=TOPICS.index(topic)+1)
    assert len(set(answers))==4
    if py and kind=='predict_output':
     result=io.StringIO()
     with contextlib.redirect_stdout(result):exec(compile(code,identity,'exec'),{})
     assert result.getvalue().rstrip('\n')==answer,(identity,result.getvalue(),answer)
    rows.append(row)

advanced={
 'objects':[
 ('What is a class used for?','A blueprint for creating objects',['A loop counter','A printed message','A comparison operator']),
 ('Two objects are made from the same class. Must their stored values always match?','No; each object can hold its own values',['Yes; all values are shared automatically','Only if the class has a name','Objects cannot store values']),
 ('What does an object method describe?','An action the object can perform',['Only the filename','The number of array indexes','A database password']),
 ('What is an object instance?','One particular object created from a class',['The class definition itself','A loop that never ends','A text quotation mark'])],
 'collections':[
 ('Which collection rule removes the last item added first?','Stack: last in, first out',['Queue: first in, first out','Alphabetical sorting','Random selection']),
 ('A queue receives A, then B, then C. Which item leaves first?','A',['B','C','All three at once']),
 ('Why use a dictionary/map for player scores?','To look up a score using a player key',['To require numeric indexes starting at zero','To repeat every statement forever','To remove every duplicate score']),
 ('What is a set useful for?','Keeping distinct values without duplicates',['Keeping a separate copy of every duplicate','Always removing the newest value','Storing only text'])],
 'exceptions':[
 ('What is a try/catch block (try/except in Python) used for?','Handling an error that happens while code runs',['Repeating a loop','Changing a value into a string','Sorting a collection']),
 ('An error occurs inside try and a matching handler exists. What happens next?','The matching catch/except handler runs',['The rest of try always runs first','The error becomes a correct answer','The program restarts automatically']),
 ('When is finally useful?','For cleanup after try and its handler, such as closing a file',['For declaring an array index','For skipping all cleanup','For making every test true']),
 ('What does throwing an exception (raising in Python) do?','Signals an error for a matching handler',['Repeats the last print','Returns a normal success value','Deletes the source code'])],
 'algorithms':[
 ('What must be true before ordinary binary search is used?','The searched values must be sorted',['The values must all be equal','The list must contain text only','The list must be empty']),
 ('Why does binary search need fewer checks than checking every item in a large sorted list?','Each comparison discards about half the remaining range',['It checks every item twice','It always guesses the correct index','It changes the target value']),
 ('What does linear search do?','Checks items one at a time until a match or the end',['Requires a recursive call for every item','Always halves the search range','Only checks the middle item']),
 ('What is the purpose of a sorting algorithm?','Arrange values in a defined order',['Remove all values','Guarantee every value is different','Convert numbers into objects'])],
 'recursion':[
 ('Why does a recursive function need a base case?','To stop making more calls for a stopping input',['To make the loop longer','To share every variable globally','To print every return twice']),
 ('A recursive countdown receives n. Which next input moves toward a base case of n <= 0?','n - 1',['n + 1','n + 2','The same n forever']),
 ('A recursive sum returns 0 for n = 0; otherwise it returns n + sum(n - 1). What is sum(3)?','6',['3','9','0']),
 ('A recursive product returns 1 for n <= 1; otherwise it returns n * product(n - 1). What is product(4)?','24',['16','10','4'])]}
for lang in ['java','csharp','python']:
 for topic,questions in advanced.items():
  for v,(prompt,answer,wrong) in enumerate(questions):
   choices=[answer]+wrong;index=(v+TOPICS.index(topic))%4;choices[0],choices[index]=choices[index],choices[0]
   rows.append(dict(id=f'curriculum-v2-{lang}-{topic}-hard-{v+1:02}',language=lang,topic=topic,difficulty='hard',min_level=level(topic,'hard'),max_level=None,question_type='multiple_choice',prompt=prompt,code_snippet='',choices=choices,correct_index=index,explanation=answer+'. Follow the stated rule one step at a time.',published=True,curriculum_order=TOPICS.index(topic)+1))

metadata=[]
for source in old:
 row={k:v for k,v in source.items() if k in ('id','language','topic','difficulty','min_level','max_level','question_type','prompt','code_snippet','choices','correct_index','explanation','published')}
 # This legacy item is basic arithmetic, not an advanced-language question.
 if row['id'].endswith('-h07'):row['difficulty']='easy'
 row['min_level']=level(row['topic'],row['difficulty']);row['max_level']=None;row['curriculum_order']=TOPICS.index(row['topic'])+1 if row['topic'] in TOPICS else 99
 metadata.append(row)
combined=metadata+rows
for row in combined:
 index=TOPICS.index(row['topic']) if row['topic'] in TOPICS else 0
 tier=max(['easy','medium','hard'].index(row['difficulty']),2 if index>=12 else 1 if index>=6 else 0)
 row['difficulty']=['easy','medium','hard'][tier]
 row['min_level']=level(row['topic'],row['difficulty'])
 row['max_level']=None
assert len({r['id'] for r in combined})==len(combined)
combined.sort(key=lambda r:(r['language'],r['curriculum_order'],['easy','medium','hard'].index(r['difficulty']),r['id']))
(ROOT/'Assets/Resources/QuestionBanks/StarterQuestions.json').write_text(json.dumps(combined,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
def quote(v):return "'"+str(v).replace("'","''")+"'"
sql=['-- Original curriculum expansion. No deletes; existing question IDs/history preserved.','begin;','alter table public.question_bank add column if not exists curriculum_order integer;', 'create index if not exists question_bank_curriculum_idx on public.question_bank(language,curriculum_order,min_level) where published;']
for row,source in zip(metadata,old):
 sql.append(f"update public.question_bank set topic={quote(row['topic'])}, difficulty={quote(row['difficulty'])}, min_level={row['min_level']}, max_level=null, curriculum_order={row['curriculum_order']} where id={quote(row['id'])} and prompt={quote(source['prompt'])} and code_snippet={quote(source['code_snippet'])} and published=true;")
columns=['id','language','topic','difficulty','min_level','max_level','question_type','prompt','code_snippet','choices','correct_index','explanation','published','curriculum_order']
sql.append('insert into public.question_bank ('+','.join(columns)+') values')
values=[]
for row in rows:
 values.append('('+','.join('null' if row[c] is None else 'true' if row[c] is True else str(row[c]) if isinstance(row[c],int) else quote(json.dumps(row[c],ensure_ascii=False))+'::jsonb' if c=='choices' else quote(row[c]) for c in columns)+')')
sql+= [',\n'.join(values),'on conflict (id) do nothing;','commit;','-- Browse in teaching order: select * from public.question_bank order by language,curriculum_order,min_level,id;']
(ROOT/'Assets/Data/Supabase/ordered_curriculum_expansion.sql').write_text('\n'.join(sql)+'\n',encoding='utf8')
print(json.dumps(dict(existing=len(old),added=len(rows),total=len(combined),python_executable_checks=sum(r['language']=='python' and r['question_type']=='predict_output' for r in rows),per_language={l:sum(r['language']==l for r in combined) for l in ['java','csharp','python']})))

