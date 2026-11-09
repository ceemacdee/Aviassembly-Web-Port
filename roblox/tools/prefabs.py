import sys,os,math
sys.path.insert(0,os.path.dirname(__file__))
import unityyaml as u, meshes as M

def v3(d,default=(0,0,0)): return (d['x'],d['y'],d['z']) if isinstance(d,dict) else default
def q4(d): return (d['x'],d['y'],d['z'],d['w']) if isinstance(d,dict) else (0,0,0,1)

class Prefab:
    def __init__(self,path):
        self.path=path; self.name=os.path.basename(path).rsplit('.',1)[0]
        self.docs=u.load(path)
        self.go={}; self.tr={}; self.comps={}  # go id -> list of (type,docid,val)
        for k,(c,t,v) in self.docs.items():
            if t=='GameObject': self.go[k]=v
        for k,(c,t,v) in self.docs.items():
            g=v.get('m_GameObject')
            if isinstance(g,dict) and str(g.get('fileID')) in self.go:
                gid=str(g['fileID'])
                name=t
                if t=='MonoBehaviour' and isinstance(v.get('m_Script'),dict): name=u.script_name(v['m_Script'].get('guid')) or 'Mono?'
                self.comps.setdefault(gid,[]).append((name,k,v))
                if t in ('Transform','RectTransform'): self.tr[k]=v
        # root transform: father 0
        self.root=None
        for k,v in self.tr.items():
            if not v.get('m_Father') or str(v['m_Father'].get('fileID'))=='0': self.root=k
        self._world={}
    def go_of_tr(self,tid): return str(self.tr[tid]['m_GameObject']['fileID'])
    def tr_of_go(self,gid):
        for name,k,v in self.comps.get(gid,[]):
            if name in ('Transform','RectTransform'): return k
    def go_name(self,gid): return str(self.go[gid].get('m_Name'))
    def active(self,gid): return self.go[gid].get('m_IsActive',1)==1
    def local(self,tid):
        t=self.tr[tid]
        return M.mat_from_trs(v3(t['m_LocalPosition']),q4(t['m_LocalRotation']),v3(t['m_LocalScale'],(1,1,1)))
    def world(self,tid):
        """matrix relative to prefab root (root's own transform excluded)"""
        if tid in self._world: return self._world[tid]
        if tid==self.root: m=M.IDENT
        else:
            f=str(self.tr[tid]['m_Father']['fileID'])
            m=M.mat_mul(self.world(f),self.local(tid))
        self._world[tid]=m; return m
    def active_in_hierarchy(self,tid):
        while True:
            if not self.active(self.go_of_tr(tid)): return False
            f=self.tr[tid].get('m_Father')
            if not f or str(f.get('fileID'))=='0': return True
            tid=str(f['fileID'])
    def path_name(self,tid):
        names=[]
        while tid!=self.root:
            names.append(self.go_name(self.go_of_tr(tid))); tid=str(self.tr[tid]['m_Father']['fileID'])
        return '/'.join(reversed(names))
    def find(self,cls):
        out=[]
        for gid,cs in self.comps.items():
            for name,k,v in cs:
                if name==cls: out.append((gid,k,v))
        return out
    def ref_tr(self,ref):
        """Transform fileID ref -> transform id"""
        if not isinstance(ref,dict): return None
        k=str(ref.get('fileID'))
        if k in self.tr: return k
        if k in self.go: return self.tr_of_go(k)
        return None
    def frame(self,tid):
        """position + axes (right, up, forward) in root space, plus scale signs"""
        m=self.world(tid)
        pos=(m[0][3],m[1][3],m[2][3])
        def col(j):
            v=(m[0][j],m[1][j],m[2][j]); l=math.sqrt(sum(x*x for x in v)) or 1
            return tuple(x/l for x in v), l
        (r,sx),(up,sy),(fw,sz)=col(0),col(1),col(2)
        det=M.mat_det(m)
        return dict(pos=pos,right=r,up=up,forward=fw,scale=(sx,sy,sz),det=det)
    def renderers(self):
        """list of (tid, mesh, materials refs, kind)"""
        out=[]
        for gid,cs in self.comps.items():
            names={n:(k,v) for n,k,v in cs}
            tid=self.tr_of_go(gid)
            if tid is None or not self.active_in_hierarchy(tid): continue
            if 'MeshFilter' in names and 'MeshRenderer' in names:
                if names['MeshRenderer'][1].get('m_Enabled',1)==0: continue
                mesh=M.mesh_from_ref(names['MeshFilter'][1].get('m_Mesh'))
                if mesh: out.append((tid,mesh,names['MeshRenderer'][1].get('m_Materials') or [],gid))
            elif 'SkinnedMeshRenderer' in names:
                v=names['SkinnedMeshRenderer'][1]
                mesh=M.mesh_from_ref(v.get('m_Mesh'))
                if mesh: out.append((tid,mesh,v.get('m_Materials') or [],gid))
        return out
    def colliders(self):
        out=[]
        for gid,cs in self.comps.items():
            tid=self.tr_of_go(gid)
            if tid is None: continue
            for name,k,v in cs:
                if name in ('BoxCollider','SphereCollider','CapsuleCollider','MeshCollider') and v.get('m_Enabled',1):
                    out.append(dict(kind=name,tid=tid,gid=gid,v=v,active=self.active_in_hierarchy(tid),go=self.go_name(gid)))
        return out

class Scene(Prefab):
    """Same as Prefab but every root keeps its own transform (scene space)."""
    def __init__(self,path):
        super().__init__(path); self.root='__scene__'
    def world(self,tid):
        if tid in self._world: return self._world[tid]
        f=self.tr[tid].get('m_Father')
        if not f or str(f.get('fileID'))=='0': m=self.local(tid)
        else: m=M.mat_mul(self.world(str(f['fileID'])),self.local(tid))
        self._world[tid]=m; return m
    def path_name(self,tid):
        names=[]
        while True:
            names.append(self.go_name(self.go_of_tr(tid)))
            f=self.tr[tid].get('m_Father')
            if not f or str(f.get('fileID'))=='0': break
            tid=str(f['fileID'])
        return '/'.join(reversed(names))
