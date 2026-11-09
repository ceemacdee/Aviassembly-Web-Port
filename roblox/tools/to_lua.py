import json,sys,re,math
OUT=__import__('os').path.join(__import__('os').path.dirname(__file__),'..','src','shared','Data')+'/'
IDENT=re.compile(r'^[A-Za-z_][A-Za-z0-9_]*$')
KEYWORDS={'and','break','do','else','elseif','end','false','for','function','if','in','local','nil','not','or','repeat','return','then','true','until','while','continue','type'}
def lua(v,ind=0):
    sp='\t'*ind
    if v is None: return 'nil'
    if isinstance(v,bool): return 'true' if v else 'false'
    if isinstance(v,int): return str(v)
    if isinstance(v,float):
        if math.isnan(v) or math.isinf(v): return '0'
        r=repr(round(v,5)); return r
    if isinstance(v,str): return json.dumps(v,ensure_ascii=False)
    if isinstance(v,(list,tuple)):
        if all(isinstance(x,(int,float)) and not isinstance(x,bool) for x in v) and len(v)<=4:
            return '{'+', '.join(lua(x) for x in v)+'}'
        if not v: return '{}'
        return '{\n'+''.join(sp+'\t'+lua(x,ind+1)+',\n' for x in v)+sp+'}'
    if isinstance(v,dict):
        if not v: return '{}'
        items=[]
        for k,x in v.items():
            if x is None: continue
            key=k if (isinstance(k,str) and IDENT.match(k) and k not in KEYWORDS) else '['+json.dumps(str(k))+']'
            items.append(sp+'\t'+key+' = '+lua(x,ind+1)+',\n')
        return '{\n'+''.join(items)+sp+'}'
    raise TypeError(type(v))

def mx(v): return [-v[0],v[1],v[2]] if v else v
def mframe(f):
    if not f: return f
    return dict(pos=mx(f['pos']),right=mx(f['right']),up=mx(f['up']),forward=mx(f['forward']),scale=f['scale'],mirrored=f['det']<0)

def write(name,data,doc):
    open(OUT+name+'.lua','w').write('-- '+doc+'\n-- Generated from the Unity project by the Aviassembly Roblox exporter. Positions are in studs (1 stud = 1 Unity metre)\n-- and converted to Roblox axes (Unity X is mirrored). Do not edit by hand; re-run the exporter instead.\nreturn '+lua(data)+'\n')

parts=json.load(open('parts.json'))
man=json.load(open('manifest.json'))
P={}
for p in parts['parts']:
    if p['name']=='Cube': continue
    comps=[]
    for c in p['components']:
        d=dict(c['data'])
        if 'orientation' in d: d['orientation']=mframe(d['orientation'])
        comps.append(dict(type=c['type'],frame=mframe(c['frame']),path=c['path'],active=c['active'],data=d))
    nodes=[]
    for n in p['nodes']:
        nm=n['name']
        if nm in man['manifest']:
            m=man['manifest'][nm]; nodes.append(dict(name=nm,file=m['file'],center=m['center'],size=m['size'],color=m['color'],alpha=m['alpha'],textured=m['textured'],go=m['go'],path=m['path']))
        k=1
        while nm+f'_{k}' in man['manifest']:
            m=man['manifest'][nm+f'_{k}']; nodes.append(dict(name=nm+f'_{k}',file=m['file'],center=m['center'],size=m['size'],color=m['color'],alpha=m['alpha'],textured=m['textured'],go=m['go'],path=m['path'])); k+=1
    b=p['bounds']
    P[p['name']]=dict(name=p['name'],displayName=p['displayName'],category=p['category'],price=p['price'],weight=round(p['weight'],4),dragFactor=p['dragFactor'],
        symetrical=p['symetrical'],flipSymmetry=p['flipSymmetry'],tryOrientUpwards=p['tryOrientUpwards'],tryMatchParentRotation=p['tryMatchParentRotation'],
        lockRotation=p['lockRotation'],basePart=p['basePart'],ignoreSnappingOnTangentPlane=p['ignoreSnappingOnTangentPlane'],forwardOrientation=mx(p['forwardOrientation']),
        snapPoints=[mframe(s) for s in p['snapPoints']],collisionPoint=mframe(p['collisionPoint']),components=comps,
        colliders=[dict(shape=c['shape'],center=mx(c['center']),right=mx(c['right']),up=mx(c['up']),size=c['size'],go=c['go'],active=c['active']) for c in p['colliders']],
        nodes=nodes,propeller=p.get('propeller'),retractable=p.get('retractable'),procedural=p.get('procedural'),
        bounds=b and dict(min=[-b['max'][0],b['min'][1],b['min'][2]],max=[-b['min'][0],b['max'][1],b['max'][2]]))
write('Parts',P,'Every buildable plane part: stats, physics components, attachment points, colliders and mesh pieces.')
write('Research',parts['research'],'Research tree: part unlock costs (scrap, advanced scrap) and tree layout.')
world=json.load(open('world.json'))
def mq(q):
    if not q: return q
    q=dict(q)
    if 'subQuests' in q: q['subQuests']=[mq(s) for s in q['subQuests']]
    if 'followup' in q: q['followup']=mq(q['followup'])
    q['id']=str(q['id'])
    return q
for c in world['continents']:
    for a in c['airports']:
        a['position']=mx(a['position']); a['quests']=[mq(q) for q in a['quests']]; a['baseQuests']=[mq(q) for q in a['baseQuests']]
        a['spawn']=man['spawnPoints'].get(a['prefab'])
        a['meshes']=[dict(name=k,file=v['file'],center=v['center'],size=v['size'],color=v['color'],alpha=v['alpha'],textured=v['textured']) for k,v in man['manifest'].items() if v['kind']=='airport' and v['owner']==a['prefab']]
    c['angle']=c['angle']; c.pop('decorations',None)
write('World',world,'Continents, airports (with their quest chains) and cargo types, from the latest world asset (version 24).')
comp=json.load(open('components.json'))
consts={k:{a:b for a,b in v[1].items() if not isinstance(b,dict) or a in ('graphicsSettings',)} for k,v in comp.items() if k in ('PlaneContainer','PlaneController','StoryManager','MoneyManager','CargoInventory','CameraController','AutoSave','FogOfWar','ScrapSpawner')}
consts['DragSimulator']=dict(defaultDrag=0.07,dragMultiplier=1.2,calculateDragWeight=0.6)
consts['Terrain']=dict(resolution=75,noiseScale=0.2,octaves=7,chunkSize=1000,defaultMinHeight=-82.5,defaultMaxHeight=167.5)
write('Constants',consts,'Tuning values copied from the Unity scenes (PlaneContainer, PlaneController, StoryManager, ...).')
print('ok', len(P),'parts')
