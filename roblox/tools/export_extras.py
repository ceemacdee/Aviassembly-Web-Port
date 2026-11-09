"""Second export pass: mirrored part meshes, decal quads and tree meshes (plus their data)."""
import sys,os,json,pickle,math
sys.path.insert(0,os.path.dirname(__file__))
import unityyaml as u, meshes as M, glb as G
from export_meshes import to_roblox, split, baked_png, add_nodes, material_info, U2R
from PIL import Image
import io

OUT=sys.argv[1]
man=json.load(open('manifest.json'))
manifest=man['manifest']

def symmetric_y(pos):
    """True when the piece looks the same mirrored top-to-bottom about its own centre."""
    ys=[p[1] for p in pos]; cy=(min(ys)+max(ys))/2
    q=lambda v: tuple(round(x,3) for x in v)
    a={q(p) for p in pos}; b={q((p[0],2*cy-p[1],p[2])) for p in pos}
    if not a: return True
    return len(a & b) / len(a) > 0.97

# 1. mirrored meshes: Unity mirrors parts with localScale.y = -1; Roblox can't, so export the flipped geometry.
geo=pickle.load(open('geo.pkl','rb'))
g=G.GLB(); count=0; files=[]
def flush():
    global g,count
    if count:
        name=f'Parts_Mirrored_{len(files)+1}'
        open(os.path.join(OUT,name+'.glb'),'wb').write(g.bytes()); files.append(name)
    g=G.GLB(); count=0
for part,nodes in sorted(geo.items()):
    for node in nodes:
        pos,nrm,uv,tris=to_roblox(node)
        if symmetric_y(pos): continue
        mnode=dict(node,name=node['name']+'_M',pos=[(p[0],-p[1],p[2]) for p in node['pos']],nrm=[(n[0],-n[1],n[2]) for n in node['nrm']] if node['nrm'] else None,
                   tris=[x for k in range(0,len(node['tris']),3) for x in (node['tris'][k],node['tris'][k+2],node['tris'][k+1])])
        if node.get('paintable'):
            pass
        add_nodes(g,[mnode],manifest,dict(kind='mirror',owner=part,file=f'Parts_Mirrored_{len(files)+1}'))
        count+=1
        if count>=70: flush()
flush()

# 2. decals: one textured 1x1 quad per decal image (the importer uploads each image; the game reuses its TextureID).
ROOT=u.ROOT
decal_names=['Circle_0','Triangle','Rect','Hexagon','TriangleRightAngle','Cresent','Diamond','Trapzium','HalfCircle','Star','CircleOutline','TriangleOutline','RectOutline','HexagonOutline','DiamondOutline','StarOutline','Electricity','Arrow','Alert','Temprature','Hearth','Star5Point','Wing 1','Wing','0','1','2','3','4','5','6','7','8','9']+[chr(c) for c in range(65,91)]
g=G.GLB(); decals=[]
for dn in decal_names:
    path=os.path.join(ROOT,'Texture2D',dn+'.png')
    if not os.path.exists(path): continue
    im=Image.open(path).convert('RGBA')
    if max(im.size)>256: im=im.resize((256,256),Image.LANCZOS)
    bio=io.BytesIO(); im.save(bio,'PNG',optimize=True)
    t=g.texture('decal_'+dn,bio.getvalue())
    mi=g.material('Decal_'+dn,[1,1,1],t,0.999)
    g.mats[mi]['alphaMode']='BLEND'
    name='Decal_'+dn.replace(' ','_')
    pos=[(-0.5,-0.5,0),(0.5,-0.5,0),(0.5,0.5,0),(-0.5,0.5,0)]
    g.add_mesh(name,pos,[(0,0,1)]*4,[(1,1),(0,1),(0,0),(1,0)],[0,1,2,0,2,3],mi)
    decals.append(dict(id=dn,mesh=name))
open(os.path.join(OUT,'Decals.glb'),'wb').write(g.bytes())

# 3. trees: every surface decoration mesh of every continent.
w=json.load(open('world.json'))
import extract_world as W
g=G.GLB(); trees={}
tree_mat=material_info({'guid':[k for k,v in u.guid_map().items() if v.endswith('Material/SurfaceDecoration.mat')][0],'fileID':2100000})
world=list(u.load(u.ROOT+'/MonoBehaviour/HelicopterUpdate.asset').values())[0][2]
deco_out={}
for cref in world['continents']:
    c=W.deref(cref); cname=c.get('continentName')
    deco_out[cname]=[]
    for sref in c.get('surfaceDecorations') or []:
        sd=W.deref(sref)
        if not sd: continue
        variants=[]
        refs=(sd.get('meshVariants') or []) or ([sd.get('mesh')] if sd.get('mesh') else [])
        for mref in refs:
            mesh=M.mesh_from_ref(mref)
            if not mesh: continue
            key='Tree_'+mesh['name'].replace(' ','_').replace('.','_')
            if key not in trees:
                for si,sub in enumerate(mesh['subs']):
                    node=dict(name=key+(f'_{si}' if si else ''),go='tree',path='',material=tree_mat,pos=[tuple(p) for p in mesh['pos']],
                              nrm=[tuple(n) for n in mesh['nrm']] if mesh['nrm'] else None,uv=[tuple(q[:2]) for q in mesh['uv']] if mesh['uv'] else None,tris=sub['indices'])
                    used=sorted(set(node['tris'])); rm={v:i for i,v in enumerate(used)}
                    node['pos']=[node['pos'][v] for v in used]
                    node['nrm']=[node['nrm'][v] for v in used] if node['nrm'] else None
                    node['uv']=[node['uv'][v] for v in used] if node['uv'] else None
                    node['tris']=[rm[x] for x in node['tris']]
                    add_nodes(g,[node],manifest,dict(kind='tree',owner=key,file='Trees'))
                trees[key]=[n for n in manifest if manifest[n].get('owner')==key and manifest[n]['kind']=='tree']
            variants.append(key)
        deco_out[cname].append(dict(name=sd['m_Name'],variants=variants,density=sd.get('density'),minSize=sd.get('minSize'),maxSize=sd.get('maxSize'),
            randomizationOffset=sd.get('randomizationOffset',0),clustering=bool(sd.get('clustering')),maxClusterSize=sd.get('maxClusterSize',1),
            minSlope=sd.get('minSlope',0),maxSlope=sd.get('maxSlope',1),height=sd.get('height'),radius=sd.get('radius'),center=bool(sd.get('center')),shade=sd.get('shade')))
open(os.path.join(OUT,'Trees.glb'),'wb').write(g.bytes())

json.dump(dict(manifest=manifest,spawnPoints=man['spawnPoints'],decals=decals,trees={k:[dict(name=n,center=manifest[n]['center'],size=manifest[n]['size'],color=manifest[n]['color'],textured=manifest[n]['textured']) for n in v] for k,v in trees.items()},decorations=deco_out),open('manifest.json','w'))
for f in sorted(os.listdir(OUT)): print(f, round(os.path.getsize(os.path.join(OUT,f))/1e6,2),'MB')
print('mirrored meshes:',sum(1 for m in manifest.values() if m['kind']=='mirror'),'decals',len(decals),'tree meshes',len(trees))
for k,v in deco_out.items(): print(k,[(d['name'],len(d['variants']),d['density']) for d in v])
