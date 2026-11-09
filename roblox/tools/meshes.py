import sys,os,struct,functools,math,re
sys.path.insert(0,os.path.dirname(__file__))
import unityyaml as u

FMT={0:('f',4),1:('e',2),2:('B',1),3:('b',1),4:('H',2),5:('h',2),6:('B',1),7:('b',1),8:('H',2),9:('h',2),10:('I',4),11:('i',4)}
NORM={2:255.0,3:127.0,4:65535.0,5:32767.0}

@functools.lru_cache(None)
def raw_mesh(path):
    text=open(path,encoding='utf8').read()
    def grab(key):
        m=re.search(key+r': ([0-9a-f]*)\n',text); return bytes.fromhex(m.group(1)) if m else b''
    d=u.load(path); v=list(d.values())[0][2]
    vd=v['m_VertexData']; n=vd['m_VertexCount']; ch=[dict(c,dimension=(c['dimension'] or 0)&15) for c in vd['m_Channels']]
    data=grab('_typelessdata'); idx=grab('m_IndexBuffer')
    # stream strides
    streams={}
    for c in ch:
        if c['dimension']:
            f,s=FMT[c['format']]
            streams.setdefault(c['stream'],0); streams[c['stream']]=max(streams[c['stream']],c['offset']+s*c['dimension'])
    # strides are per-stream max end rounded to 4; stream base offsets sequential
    base={};off=0
    for s in sorted(streams):
        stride=(streams[s]+3)//4*4; base[s]=(off,stride); off+=((stride*n)+15)//16*16
    def channel(i):
        c=ch[i] if i<len(ch) else None
        if not c or not c['dimension']: return None
        f,size=FMT[c['format']]; b0,stride=base[c['stream']]
        out=[]
        for k in range(n):
            p=b0+stride*k+c['offset']
            vals=struct.unpack_from('<'+f*c['dimension'],data,p)
            if c['format'] in NORM: vals=tuple(x/NORM[c['format']] for x in vals)
            out.append(vals)
        return out
    pos=channel(0); nrm=channel(1); col=channel(3); uv=channel(4)
    i32=v.get('m_IndexFormat',0)==1
    fmt='<I' if i32 else '<H'; isz=4 if i32 else 2
    subs=[]
    for sm in v['m_SubMeshes']:
        first=sm['firstByte']//isz; cnt=sm['indexCount']
        tris=[struct.unpack_from(fmt,idx,(first+k)*isz)[0]+sm.get('baseVertex',0) for k in range(cnt)]
        subs.append(dict(topology=sm['topology'],indices=tris))
    return dict(name=v['m_Name'],pos=pos,nrm=nrm,uv=uv,col=col,subs=subs)

def mesh_from_ref(ref):
    if not isinstance(ref,dict) or not ref.get('guid'): return None
    p=u.guid_map().get(ref['guid'])
    if not p or not p.endswith('.asset'): return None
    return raw_mesh(p)

# ---------- math (Unity space) ----------
def qmul(a,b):
    ax,ay,az,aw=a; bx,by,bz,bw=b
    return (aw*bx+ax*bw+ay*bz-az*by, aw*by-ax*bz+ay*bw+az*bx, aw*bz+ax*by-ay*bx+az*bw, aw*bw-ax*bx-ay*by-az*bz)
def qrot(q,v):
    x,y,z,w=q; vx,vy,vz=v
    # v' = q v q*
    ix= w*vx + y*vz - z*vy; iy= w*vy + z*vx - x*vz; iz= w*vz + x*vy - y*vx; iw=-x*vx - y*vy - z*vz
    return (ix*w + iw*-x + iy*-z - iz*-y, iy*w + iw*-y + iz*-x - ix*-z, iz*w + iw*-z + ix*-y - iy*-x)
def mat_from_trs(t,q,s):
    # 3x4 matrix columns = rotated scaled axes
    ex=qrot(q,(s[0],0,0)); ey=qrot(q,(0,s[1],0)); ez=qrot(q,(0,0,s[2]))
    return [[ex[0],ey[0],ez[0],t[0]],[ex[1],ey[1],ez[1],t[1]],[ex[2],ey[2],ez[2],t[2]]]
def mat_mul(a,b):
    r=[[0]*4 for _ in range(3)]
    for i in range(3):
        for j in range(4):
            r[i][j]=sum(a[i][k]*b[k][j] for k in range(3))+(a[i][3] if j==3 else 0)
    return r
def mat_point(m,p): return tuple(m[i][0]*p[0]+m[i][1]*p[1]+m[i][2]*p[2]+m[i][3] for i in range(3))
def mat_dir(m,p): return tuple(m[i][0]*p[0]+m[i][1]*p[1]+m[i][2]*p[2] for i in range(3))
def mat_det(m):
    return (m[0][0]*(m[1][1]*m[2][2]-m[1][2]*m[2][1])-m[0][1]*(m[1][0]*m[2][2]-m[1][2]*m[2][0])+m[0][2]*(m[1][0]*m[2][1]-m[1][1]*m[2][0]))
def mat_inv_t3(m):
    # inverse transpose of 3x3 for normals
    a=[[m[i][j] for j in range(3)] for i in range(3)]; d=mat_det(m) or 1e-12
    inv=[[ (a[(j+1)%3][(i+1)%3]*a[(j+2)%3][(i+2)%3]-a[(j+1)%3][(i+2)%3]*a[(j+2)%3][(i+1)%3])/d for j in range(3)] for i in range(3)]
    return [[inv[j][i] for j in range(3)] for i in range(3)]
IDENT=[[1,0,0,0],[0,1,0,0],[0,0,1,0]]
