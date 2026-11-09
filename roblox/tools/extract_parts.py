import sys,os,json,glob,math
sys.path.insert(0,os.path.dirname(__file__))
import unityyaml as u, prefabs as P, meshes as M

CATS=[('Cockpits',['Body','BiplaneCockpit','Cockpit','JetCockpit','PrivateJetCockpit','BigCockpit','SmallHelicopterCockpit','SmallLargeCockpit','JetlineCockpit','GliderCockpit','JetCockpit02','SmallPlaneCockpit','AreoDynamicCockpit','HugeCockpit','HelicopterCockpit']),
 ('Engines',['WoodEngine','Engine','SmallPropEngine','TurboProp','SmallJetEngine','JetEngine','FighterJetEngine','RocketEngine','ElectricEngine','BigElectricEngine','PropfanEngine','RamJet','HelicopterEngine','HelicopterEngineSmall','HugePropEngine']),
 ('Wings',['BiplaneWing','TailWing','Wing','ElectronWing','StraightWing','SmallJetWing','BigWing','Tailwing02','DeltaWing01','Tailwing03','DeltaWing02','Tailwing04','SmallCanard','SmallRecreationalWing','GliderWing','WoodWing']),
 ('Wheels',['BikeWheel','SmallRearWheel','RearWheel','OldSteelWheel','BigOldWheel','AeroWheel','AeroWheelLong','SmallRetractableGear','Wheel03','BigRetractableGear','TinyRetractableGear','Wheel01','HelicopterLandingGear']),
 ('Utility',['WoodStrut','WoodConnection','SmallFuelTank','ExternalFuelTank','FuelTank','BigExternalFuelTank','SmallBalloon','BigBalloon','Parachute','Airbreak','AirIntake','AirIntake2','AirOutlet']),
 ('Electric',['SmallBatteries','BigBatteries','RotationJoint','Decoupler','SolarPanel','AirTurbine','Camera','TempratureFan'])]
cat_of={n:c for c,ns in CATS for n in ns}
PLANEPART={'Engine','Wing','FuelTank','Fuselage','Battery','Balloon','Parachute','Airbreak','Wheel','Decoupler','Rotator','AirTurbine','BasicPart','CameraPart','CoolingFan'}

def r4(x): return round(x,4)
def vec(t): return [r4(x) for x in t]
def frame(p,tid):
    f=p.frame(tid); return dict(pos=vec(f['pos']),right=vec(f['right']),up=vec(f['up']),forward=vec(f['forward']),scale=vec(f['scale']),det=r4(f['det']))

def curve(c):
    if not isinstance(c,dict): return []
    return [[k['time'],k['value'],k.get('inSlope',0),k.get('outSlope',0)] for k in (c.get('m_Curve') or [])]

def material_info(ref):
    if not isinstance(ref,dict) or not ref.get('guid'): return None
    p=u.guid_map().get(ref['guid'])
    if not p or not p.endswith('.mat'): return None
    d=list(u.load(p).values())[0][2]
    props=d.get('m_SavedProperties',{})
    cols=props.get('m_Colors') or {}; fl=props.get('m_Floats') or {}; tex=props.get('m_TexEnvs') or {}
    mt=tex.get('_MainTex') or {}
    tpath=u.guid_map().get((mt.get('m_Texture') or {}).get('guid',''),'')
    c=cols.get('_Color') or {'r':1,'g':1,'b':1,'a':1}
    sp=u.guid_map().get(d['m_Shader'].get('guid',''),'') if isinstance(d.get('m_Shader'),dict) else ''
    return dict(name=d['m_Name'],color=[r4(c['r']),r4(c['g']),r4(c['b']),r4(c['a'])],texture=tpath if tpath.endswith(('.png','.jpg','.tga')) else None,
        texScale=[mt.get('m_Scale',{}).get('x',1),mt.get('m_Scale',{}).get('y',1)],texOffset=[mt.get('m_Offset',{}).get('x',0),mt.get('m_Offset',{}).get('y',0)],
        textureOpacity=fl.get('_TextureOpacity',1),shade=fl.get('_Shade'),smoothness=fl.get('_Smoothness'),shader=os.path.basename(sp),
        transparent='Transparent' in os.path.basename(sp) or (d.get('m_CustomRenderQueue',-1) or -1)>=3000)

def collect_geometry(p, prefix):
    """returns list of nodes: name, tid path, mesh world-space (root space) geometry per material"""
    nodes=[]
    for i,(tid,mesh,mats,gid) in enumerate(p.renderers()):
        m=p.world(tid)
        nt=M.mat_inv_t3(m); flip=M.mat_det(m)<0
        for si,sub in enumerate(mesh['subs']):
            if sub['topology']!=0: continue
            mat=material_info(mats[si] if si<len(mats) else (mats[-1] if mats else None))
            used=sorted(set(sub['indices']))
            remap={v:k for k,v in enumerate(used)}
            pos=[M.mat_point(m,mesh['pos'][v]) for v in used]
            nrm=[M.mat_dir(nt,mesh['nrm'][v]) for v in used] if mesh['nrm'] else None
            uv=[mesh['uv'][v][:2] for v in used] if mesh['uv'] else None
            tris=[remap[x] for x in sub['indices']]
            if flip: tris=[x for k in range(0,len(tris),3) for x in (tris[k],tris[k+2],tris[k+1])]
            nodes.append(dict(name=f"{prefix}__{len(nodes)}",go=p.go_name(gid),path=p.path_name(tid),material=mat,pos=pos,nrm=nrm,uv=uv,tris=tris))
    return nodes

def bounds(nodes):
    xs=[q for n in nodes for q in n['pos']]
    if not xs: return None
    mn=[min(v[i] for v in xs) for i in range(3)]; mx=[max(v[i] for v in xs) for i in range(3)]
    return dict(min=vec(mn),max=vec(mx))

def collider_box(p,c):
    """convert collider to oriented box/sphere in root space"""
    m=p.world(c['tid']); v=c['v']
    f=p.frame(c['tid'])
    if c['kind']=='BoxCollider':
        cen=M.mat_point(m,P.v3(v.get('m_Center')))
        s=P.v3(v.get('m_Size'),(1,1,1))
        size=[abs(s[i]*f['scale'][i]) for i in range(3)]
        return dict(shape='box',center=vec(cen),right=vec(f['right']),up=vec(f['up']),size=vec(size))
    if c['kind']=='SphereCollider':
        cen=M.mat_point(m,P.v3(v.get('m_Center'))); r=v.get('m_Radius',0.5)*max(abs(x) for x in f['scale'])
        return dict(shape='ball',center=vec(cen),right=vec(f['right']),up=vec(f['up']),size=[r4(2*r)]*3)
    if c['kind']=='CapsuleCollider':
        cen=M.mat_point(m,P.v3(v.get('m_Center'))); r=v.get('m_Radius',0.5); h=v.get('m_Height',1); d=v.get('m_Direction',1)
        s=[2*r,2*r,2*r]; s[d]=max(h,2*r); size=[abs(s[i]*f['scale'][i]) for i in range(3)]
        return dict(shape='box',center=vec(cen),right=vec(f['right']),up=vec(f['up']),size=vec(size))
    if c['kind']=='MeshCollider':
        mesh=M.mesh_from_ref(v.get('m_Mesh'))
        if not mesh: return None
        pts=mesh['pos']
        mn=[min(q[i] for q in pts) for i in range(3)]; mx=[max(q[i] for q in pts) for i in range(3)]
        cen=M.mat_point(m,[(mn[i]+mx[i])/2 for i in range(3)])
        size=[abs((mx[i]-mn[i])*f['scale'][i]) for i in range(3)]
        return dict(shape='box',center=vec(cen),right=vec(f['right']),up=vec(f['up']),size=vec(size))

def comp_data(p,cls,v):
    d={k:v2 for k,v2 in v.items() if not k.startswith('m_') and not isinstance(v2,dict) and not isinstance(v2,list)}
    for k in ('rb','controller','settingsPrefab','keybindSettingsPrefab'): d.pop(k,None)
    if cls=='Wing':
        d['area']=[v['area']['x'],v['area']['y']]
        t=p.ref_tr(v.get('orientation')); d['orientation']=frame(p,t) if t else None
    if cls=='Engine':
        d['maxSpeed']=curve(v.get('maxSpeed'))
    if cls in ('Parachute','Airbreak'):
        key='parachuteTransform' if cls=='Parachute' else 'airbreakTransform'
        t=p.ref_tr(v.get(key)); d['visual']=p.path_name(t) if t else None
    if cls=='Rotator':
        t=p.ref_tr(v.get('rotatorBase')); d['rotatorBase']=p.path_name(t) if t else None
    if cls=='AirTurbine':
        t=p.ref_tr(v.get('rotor')); d['rotor']=p.path_name(t) if t else None
    return d

def extract(name):
    p=P.Prefab(u.ROOT+f'/GameObject/{name}.prefab')
    bp=p.find('BuildingPart')[0]
    b=bp[2]
    part=dict(name=name,category=cat_of.get(name,'Cockpits'),displayName=b.get('partName') or name,price=b.get('price',0),
        symetrical=bool(b.get('symetrical')),flipSymmetry=bool(b.get('flipSymmetry')),tryOrientUpwards=bool(b.get('tryOrientUpwards')),
        tryMatchParentRotation=bool(b.get('tryMatchParentRotation')),lockRotation=bool(b.get('lockRotation')),basePart=bool(b.get('basePart')),
        ignoreSnappingOnTangentPlane=bool(b.get('ignoreSnappingOnTangentPlane')),forwardOrientation=vec(P.v3(b.get('forwardOrientation'))),radius=b.get('radius',0),
        snapPoints=[frame(p,t) for t in (p.ref_tr(s) for s in (b.get('snapPoints') or [])) if t],
        collisionPoint=frame(p,p.ref_tr(b.get('collisionPoint'))) if p.ref_tr(b.get('collisionPoint')) else None,
        components=[],weight=0,dragFactor=0)
    for gid,cs in p.comps.items():
        tid=p.tr_of_go(gid)
        for cls,k,v in cs:
            if cls in PLANEPART:
                part['weight']+=v.get('weight',0) or 0
                part['components'].append(dict(type=cls,frame=frame(p,tid),path=p.path_name(tid),active=p.active_in_hierarchy(tid),data=comp_data(p,cls,v)))
            if cls=='PartDrag': part['dragFactor']=v.get('dragFactor',0)
            if cls=='PropellerVisual':
                t=p.ref_tr(v.get('propeller')); part['propeller']=dict(path=p.path_name(t) if t else None,speed=v.get('propSpeed'))
            if cls=='RetractableLandingGear': part['retractable']=dict(targetAngle=v.get('targetAngle'),disableAngle=v.get('disableAngle'),animationSpeed=v.get('animationSpeed'),
                    upper=p.path_name(p.ref_tr(v.get('upperSuspension'))) if p.ref_tr(v.get('upperSuspension')) else None)
            if cls=='ProceduralFuselage': part['procedural']=True
    nodes=collect_geometry(p,name)
    part['nodes']=[dict(name=n['name'],go=n['go'],path=n['path'],material=n['material'] and {k:n['material'][k] for k in ('name','color','textureOpacity','transparent')}) for n in nodes]
    part['bounds']=bounds(nodes)
    part['colliders']=[x for x in (dict(collider_box(p,c) or {},go=c['go'],active=c['active']) for c in p.colliders()) if x.get('shape')]
    return part,nodes

if __name__=='__main__':
    names=sorted(set(n for c,ns in CATS for n in ns)|{os.path.basename(f)[:-7] for f in glob.glob(u.ROOT+'/GameObject/*.prefab') if 'BuildingPart' in open(f).read() or True} )
    out=[];geo={}
    for f in sorted(glob.glob(u.ROOT+'/GameObject/*.prefab')):
        n=os.path.basename(f)[:-7]
        p=P.Prefab(f)
        if not p.find('BuildingPart'): continue
        part,nodes=extract(n); out.append(part); geo[n]=nodes
    # research tree
    rt=list(u.load(u.ROOT+'/MonoBehaviour/ResearchTree.asset').values())[0][2]
    import extract_world as W
    items=[]
    for ref in rt['items']:
        it=W.deref(ref)
        pf=u.guid_map().get((it.get('Prefab') or {}).get('guid',''),'')
        items.append(dict(id=it['m_Name'],part=os.path.basename(pf).replace('.prefab','') if pf else None,cost1=it.get('Cost1',0),cost2=it.get('Cost2',0),
            children=[ (W.deref(c) or {}).get('m_Name') for c in (it.get('children') or [])],x=(it.get('window') or {}).get('x',0),y=(it.get('window') or {}).get('y',0)))
    json.dump(dict(parts=out,research=items),open(sys.argv[1],'w'),indent=1)
    import pickle; pickle.dump(geo,open(sys.argv[2],'wb'))
    print(len(out),'parts',len(items),'research items',sum(len(n['tris'])//3 for g in geo.values() for n in g),'triangles total')
    for pt in out: print(f"{pt['name']:24s} {pt['category']:9s} w={pt['weight']:6.2f} price={pt['price']:6} nodes={len(pt['nodes'])} cols={len(pt['colliders'])} snaps={len(pt['snapPoints'])} cp={'Y' if pt['collisionPoint'] else 'n'} tris={sum(len(n['tris'])//3 for n in geo[pt['name']])} comps={[c['type'] for c in pt['components']]}")
