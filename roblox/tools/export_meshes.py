import sys,os,json,pickle,io,math
sys.path.insert(0,os.path.dirname(__file__))
import unityyaml as u, prefabs as P, meshes as M, glb as G
from extract_parts import collect_geometry, material_info
from PIL import Image

OUT=sys.argv[1]; os.makedirs(OUT,exist_ok=True)
MAXTRI=19000

def U2R(p): return (-p[0],p[1],p[2])
_texcache={}
def baked_png(mat):
    tex=mat.get('texture'); op=mat.get('textureOpacity',1) or 0
    if not tex or op<=0.01: return None
    key=(tex,tuple(mat['color']),round(op,3))
    if key in _texcache: return _texcache[key]
    im=Image.open(tex).convert('RGBA')
    if max(im.size)>512: im=im.resize((min(512,im.size[0]),min(512,im.size[1])),Image.LANCZOS)
    import numpy as np
    a=np.asarray(im).astype('float32')/255.0
    col=np.array(mat['color'][:3],dtype='float32')
    rgb=(1.0+(a[...,:3]-1.0)*op)*col
    out=np.concatenate([np.clip(rgb,0,1),a[...,3:4]],axis=-1)
    bio=io.BytesIO(); Image.fromarray((out*255+0.5).astype('uint8'),'RGBA').save(bio,'PNG',optimize=True)
    _texcache[key]=(os.path.basename(tex)+f"_{len(_texcache)}",bio.getvalue()); return _texcache[key]

def to_roblox(node):
    pos=[U2R(p) for p in node['pos']]
    nrm=[U2R(n) for n in node['nrm']] if node['nrm'] else None
    if nrm: nrm=[(lambda l:(x/l,y/l,z/l))(math.sqrt(x*x+y*y+z*z) or 1) for x,y,z in nrm]
    uv=None
    m=node['material'] or {}
    if node['uv']:
        sx,sy=m.get('texScale',[1,1]); ox,oy=m.get('texOffset',[0,0])
        uv=[(q[0]*sx+ox,1.0-(q[1]*sy+oy)) for q in node['uv']]
    t=node['tris']; tris=[x for k in range(0,len(t),3) for x in (t[k],t[k+2],t[k+1])]
    return pos,nrm,uv,tris

def split(pos,nrm,uv,tris):
    """split into <=MAXTRI triangle chunks"""
    if len(tris)//3<=MAXTRI: yield pos,nrm,uv,tris; return
    for s in range(0,len(tris),MAXTRI*3):
        sub=tris[s:s+MAXTRI*3]; used=sorted(set(sub)); rm={v:i for i,v in enumerate(used)}
        yield [pos[v] for v in used],([nrm[v] for v in used] if nrm else None),([uv[v] for v in used] if uv else None),[rm[x] for x in sub]

def add_nodes(g,nodes,manifest,prefix_meta):
    for node in nodes:
        mat=node['material'] or dict(name='default',color=[0.8,0.8,0.8,1],textureOpacity=0,transparent=False)
        pos,nrm,uv,tris=to_roblox(node)
        for ci,(cp,cn,cu,ct) in enumerate(split(pos,nrm,uv,tris)):
            name=node['name']+(f"_{ci}" if ci else "")
            png=baked_png(mat)
            tex=g.texture(*png) if (png and cu) else None
            alpha=mat['color'][3] if mat.get('transparent') else 1.0
            mi=g.material(mat['name'],[1,1,1] if tex is not None else mat['color'][:3],tex,alpha)
            g.add_mesh(name,cp,cn,cu if tex is not None else None,ct,mi)
            mn=[min(p[i] for p in cp) for i in range(3)]; mx=[max(p[i] for p in cp) for i in range(3)]
            manifest[name]=dict(prefix_meta,center=[round((mn[i]+mx[i])/2,4) for i in range(3)],size=[round(max(mx[i]-mn[i],0.002),4) for i in range(3)],
                color=[round(c,4) for c in mat['color'][:3]],alpha=round(alpha,3),textured=tex is not None,go=node.get('go'),path=node.get('path'),tris=len(ct)//3)

def merged_env_nodes(p,prefix):
    """merge static environment renderers by material (keeps file small and part count low)"""
    nodes=collect_geometry(p,prefix)
    groups={}
    for n in nodes:
        if any(abs(c)>5000 for q in n['pos'] for c in q): continue  # stray/hidden objects far outside the airport
        key=(n['material'] or {}).get('name','default')
        g=groups.setdefault(key,dict(name=None,go='env',path='',material=n['material'],pos=[],nrm=[],uv=[],tris=[],hasn=True,hasuv=True))
        off=len(g['pos'])
        g['pos']+=n['pos']; g['tris']+=[t+off for t in n['tris']]
        g['nrm']+=n['nrm'] or [(0,1,0)]*len(n['pos'])
        g['uv']+=n['uv'] or [(0,0)]*len(n['pos'])
    out=[]
    for i,(k,g) in enumerate(groups.items()):
        g['name']=f"{prefix}__{i}"; out.append(g)
    return out

if __name__=='__main__':
    parts=json.load(open('parts.json'))['parts']
    geo=pickle.load(open('geo.pkl','rb'))
    manifest={}
    batches={}
    for pt in parts:
        batches.setdefault('Parts_'+pt['category'],[]).append(pt['name'])
    for b,names in batches.items():
        g=G.GLB()
        for n in names: add_nodes(g,geo[n],manifest,dict(kind='part',owner=n,file=b))
        open(os.path.join(OUT,b+'.glb'),'wb').write(g.bytes())
    world=json.load(open('world.json'))
    envs=sorted({a['prefab'] for c in world['continents'] for a in c['airports']})
    spawn={}
    groups=[envs[i:i+6] for i in range(0,len(envs),6)]
    for gi,grp in enumerate(groups):
        g=G.GLB(); fname=f'Airports_{gi+1}'
        for n in grp:
            p=P.Prefab(u.ROOT+'/GameObject/'+n+'.prefab')
            add_nodes(g,merged_env_nodes(p,'A_'+n.replace(' ','')),manifest,dict(kind='airport',owner=n,file=fname))
            sp=p.find('AirportSpawnPoint')
            if sp:
                t=p.ref_tr(sp[0][2].get('spawnPoint')) or p.tr_of_go(sp[0][0])
                f=p.frame(t); spawn[n]=dict(pos=[round(x,3) for x in U2R(f['pos'])],forward=[round(x,4) for x in U2R(f['forward'])])
        open(os.path.join(OUT,fname+'.glb'),'wb').write(g.bytes())
    json.dump(dict(manifest=manifest,spawnPoints=spawn),open('manifest.json','w'))
    for f in sorted(os.listdir(OUT)): print(f, round(os.path.getsize(os.path.join(OUT,f))/1e6,2),'MB', sum(1 for m in manifest.values() if m['file']+'.glb'==f),'meshes')
