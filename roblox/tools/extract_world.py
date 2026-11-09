import sys,os,json; sys.path.insert(0,os.path.dirname(__file__))
import unityyaml as u

def deref(ref):
    if not isinstance(ref,dict) or not ref.get('guid'): return None
    path=u.guid_map().get(ref['guid'])
    if not path: return None
    d=u.load(path)
    doc=d.get(str(ref['fileID']))
    if doc is None and len(d)==1: doc=list(d.values())[0]
    if doc is None: return None
    v=dict(doc[2]); v['__file']=os.path.basename(path); v['__type']=u.script_name(v['m_Script']['guid']) if isinstance(v.get('m_Script'),dict) else doc[1]
    return v

def color(c): return [round(c['r'],4),round(c['g'],4),round(c['b'],4)] if isinstance(c,dict) else None

def cargo(ref):
    c=deref(ref)
    if not c: return None
    return c['m_Name']

cargo_types={}
def cargo_full(ref):
    c=deref(ref)
    if not c: return None
    name=c['m_Name']
    if name not in cargo_types:
        icon=u.guid_map().get((c.get('icon') or {}).get('guid',''),'')
        cargo_types[name]=dict(id=name,name=c.get('cargoName') or name,basePrice=c['basePrice'],weight=c['weight'],cargoSpace=c['cargoSpace'],
            fragile=bool(c['fragile']),expires=bool(c['expires']),expirationTime=c['expirationTime'],icon=os.path.basename(icon))
    return name

def quest(ref, seen=None):
    q=deref(ref)
    if not q: return None
    out=dict(type=q['__type'],id=q['m_Name'],name=q.get('questName') or '',storyState=q.get('storyState'),description=q.get('description') or '',
        giveMessage=q.get('giveMessage') or '',completeMessage=q.get('completeMessage') or '',scrap=q.get('researchPointReward',0),advancedScrap=q.get('advancedReseachPointReward',0))
    if q['__type']=='DeliveryQuestData':
        out.update(cargo=cargo_full(q.get('cargoType')),amount=q.get('amount'),rewardOffset=q.get('rewardOffset',0))
    elif q['__type']=='ContainerQuestData':
        out['subQuests']=[x for x in (quest(s) for s in (q.get('subQuests') or [])) if x]
    if q.get('followupQuest') and q['followupQuest'].get('guid'):
        out['followup']=quest(q['followupQuest'])
    return out

def airport(ref):
    a=deref(ref)
    ew=a.get('editorWindow') or {}
    prefab=u.guid_map().get((a.get('airportPrefab') or {}).get('guid',''),'')
    return dict(id=a['m_Name'],name=a.get('airportName') or a['m_Name'],prefab=os.path.basename(prefab).replace('.prefab',''),
        lockedCargo=bool(a.get('lockedCargo')),refuel=bool(a.get('refuelAvailable')),cargoTypes=[cargo_full(c) for c in (a.get('cargoTypes') or []) if cargo_full(c)],
        baseAirport=bool(a.get('baseAirport')),initialSpawn=bool(a.get('initialSpawnAirport')),rotationOverride=bool(a.get('rotationOverride')),rotation=a.get('rotation',0),
        footprint=[a['footprint']['x'],a['footprint']['y']],blendingDistance=a.get('blendingDistance',0),offshore=bool(a.get('offshoreAirport')),
        position=[ew.get('x',0)*10,0,-ew.get('y',0)*10],landingMessage=a.get('landingMessage') or '',
        quests=[x for x in (quest(q) for q in (a.get('quests') or [])) if x],baseQuests=[x for x in (quest(q) for q in (a.get('baseQuests') or [])) if x])

def material_colors(ref):
    m=deref(ref)
    if not m: return None
    cols=m.get('m_SavedProperties',{}).get('m_Colors',{}) or {}
    fl=m.get('m_SavedProperties',{}).get('m_Floats',{}) or {}
    return dict(grass=color(cols.get('_GrassColor')),sand=color(cols.get('_SandColor')),dirt=color(cols.get('_DirtColor')),rock=color(cols.get('_RockColor')),noise=fl.get('_NoiceStrength'),shade=fl.get('_Shade'))

world=u.load(u.ROOT+'/MonoBehaviour/HelicopterUpdate.asset'); w=list(world.values())[0][2]
continents=[]
for cref in w['continents']:
    c=deref(cref)
    fog=deref(c.get('fogProfile'))
    decos=[]
    for s in c.get('surfaceDecorations') or []:
        sd=deref(s)
        if sd: decos.append({k:v for k,v in sd.items() if not k.startswith('m_') and not k.startswith('__') and not isinstance(v,dict)}|{'name':sd['m_Name']})
    continents.append(dict(id=c['m_Name'],name=c.get('continentName'),angle=c.get('angleFromOrigin'),distance=c.get('distanceFromOrigin'),radius=c.get('radius'),
        minHeight=c.get('minHeight'),maxHeight=c.get('maxHeight'),surfaceColor=color(c.get('surfaceColor')),temperature=c.get('temprature'),scrapAmount=c.get('scrapAmount'),
        hintDistance=c.get('hintDistance'),terrain=material_colors(c.get('terrainMaterial')),decorations=decos,
        fog=dict(fogColor=color(fog.get('fogColor')),skyColor=color(fog.get('skyColor')),exponent=fog.get('exponent')) if fog else None,
        airports=[airport(a) for a in c.get('airports') or []]))
for cref in w['cargoTypes']: cargo_full(cref)
out=dict(continents=continents,cargoTypes=list(cargo_types.values()))
json.dump(out,open(sys.argv[1],'w'),indent=1)
print(len(continents),'continents',sum(len(c['airports']) for c in continents),'airports',len(cargo_types),'cargo')
for c in continents: print(c['name'],c['radius'],[a['name'] for a in c['airports']])
