"""Tiny binary glTF 2.0 writer: one node per mesh, one primitive per node, embedded PNG textures."""
import json,struct,io

class GLB:
    def __init__(self):
        self.bin=bytearray(); self.views=[]; self.acc=[]; self.meshes=[]; self.nodes=[]; self.mats=[]; self.images=[]; self.textures=[]; self.samplers=[{"magFilter":9729,"minFilter":9987,"wrapS":10497,"wrapT":10497}]
        self._tex_cache={}
    def _view(self,data,target=None):
        while len(self.bin)%4: self.bin.append(0)
        off=len(self.bin); self.bin+=data
        v={"buffer":0,"byteOffset":off,"byteLength":len(data)}
        if target: v["target"]=target
        self.views.append(v); return len(self.views)-1
    def _accessor(self,view,ctype,count,typ,mn=None,mx=None):
        a={"bufferView":view,"componentType":ctype,"count":count,"type":typ}
        if mn is not None: a["min"]=mn; a["max"]=mx
        self.acc.append(a); return len(self.acc)-1
    def texture(self,key,png_bytes):
        if key in self._tex_cache: return self._tex_cache[key]
        v=self._view(png_bytes); self.images.append({"bufferView":v,"mimeType":"image/png","name":key})
        self.textures.append({"sampler":0,"source":len(self.images)-1}); t=len(self.textures)-1
        self._tex_cache[key]=t; return t
    def material(self,name,color,tex=None,alpha=1.0):
        m={"name":name,"pbrMetallicRoughness":{"baseColorFactor":[color[0],color[1],color[2],alpha],"metallicFactor":0.0,"roughnessFactor":0.9},"doubleSided":True}
        if tex is not None: m["pbrMetallicRoughness"]["baseColorTexture"]={"index":tex}
        if alpha<0.999: m["alphaMode"]="BLEND"
        self.mats.append(m); return len(self.mats)-1
    def add_mesh(self,name,pos,nrm,uv,tris,mat):
        n=len(pos)
        pb=b''.join(struct.pack('<3f',*p) for p in pos)
        mn=[min(p[i] for p in pos) for i in range(3)]; mx=[max(p[i] for p in pos) for i in range(3)]
        attrs={"POSITION":self._accessor(self._view(pb,34962),5126,n,"VEC3",mn,mx)}
        if nrm:
            nb=b''.join(struct.pack('<3f',*q) for q in nrm); attrs["NORMAL"]=self._accessor(self._view(nb,34962),5126,n,"VEC3")
        if uv:
            ub=b''.join(struct.pack('<2f',*q) for q in uv); attrs["TEXCOORD_0"]=self._accessor(self._view(ub,34962),5126,n,"VEC2")
        big=n>65535
        ib=b''.join(struct.pack('<I' if big else '<H',i) for i in tris)
        ind=self._accessor(self._view(ib,34963),5125 if big else 5123,len(tris),"SCALAR")
        self.meshes.append({"name":name,"primitives":[{"attributes":attrs,"indices":ind,"material":mat}]})
        self.nodes.append({"name":name,"mesh":len(self.meshes)-1})
    def bytes(self):
        while len(self.bin)%4: self.bin.append(0)
        doc={"asset":{"version":"2.0","generator":"aviassembly-roblox-export"},"scene":0,"scenes":[{"nodes":list(range(len(self.nodes)))}],
             "nodes":self.nodes,"meshes":self.meshes,"accessors":self.acc,"bufferViews":self.views,"buffers":[{"byteLength":len(self.bin)}],"materials":self.mats}
        if self.images: doc.update(images=self.images,textures=self.textures,samplers=self.samplers)
        js=json.dumps(doc,separators=(',',':')).encode()
        while len(js)%4: js+=b' '
        total=12+8+len(js)+8+len(self.bin)
        return struct.pack('<III',0x46546C67,2,total)+struct.pack('<II',len(js),0x4E4F534A)+js+struct.pack('<II',len(self.bin),0x004E4942)+bytes(self.bin)
