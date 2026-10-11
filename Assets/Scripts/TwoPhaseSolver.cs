using System;
using System.Collections.Generic;
using Stopwatch = System.Diagnostics.Stopwatch;
using UnityEngine;

// Two-phase IDA* in the project's confirmed position/orientation convention.
// Tables are generated cooperatively, cached for this play session, and are
// never loaded from a third-party move convention. Output uses quarter turns.
public static class TwoPhaseSolver
{
    private static readonly string[] CornerIDs = { "URF", "URB", "ULF", "ULB", "DRF", "DRB", "DLF", "DLB" };
    private static readonly string[] EdgeIDs = { "UF", "UR", "UB", "UL", "DF", "DR", "DB", "DL", "RF", "LF", "RB", "LB" };
    private static readonly Vector3Int[] CornerPositions = {
        new Vector3Int(1,1,1), new Vector3Int(1,1,-1), new Vector3Int(-1,1,1), new Vector3Int(-1,1,-1),
        new Vector3Int(1,-1,1), new Vector3Int(1,-1,-1), new Vector3Int(-1,-1,1), new Vector3Int(-1,-1,-1) };
    private static readonly Vector3Int[] EdgePositions = {
        new Vector3Int(0,1,1), new Vector3Int(1,1,0), new Vector3Int(0,1,-1), new Vector3Int(-1,1,0),
        new Vector3Int(0,-1,1), new Vector3Int(1,-1,0), new Vector3Int(0,-1,-1), new Vector3Int(-1,-1,0),
        new Vector3Int(1,0,1), new Vector3Int(-1,0,1), new Vector3Int(1,0,-1), new Vector3Int(-1,0,-1) };
    private static readonly string[] Faces = { "U", "D", "R", "L", "F", "B" };
    private static readonly int[] Phase2Moves = { 0,1,2,3,4,5,7,10,13,16 };
    private static readonly int[] Factorial = { 1,1,2,6,24,120,720,5040,40320 };
    private static Tables sharedTables;
    private static IEnumerator<int> sharedBuilder;
    public static bool TablesReady => sharedTables != null && sharedTables.Ready;

    private sealed class State
    {
        public readonly byte[] cp = new byte[8], co = new byte[8], ep = new byte[12], eo = new byte[12];
        public State() { Reset(); }
        public void Reset()
        {
            for (int i=0;i<8;i++) { cp[i]=(byte)i; co[i]=0; }
            for (int i=0;i<12;i++) { ep[i]=(byte)i; eo[i]=0; }
        }
    }

    private static void Apply(State source, State move, State target)
    {
        for (int i=0;i<8;i++) {
            int from=move.cp[i]; target.cp[i]=source.cp[from];
            target.co[i]=(byte)((source.co[from]+move.co[i])%3);
        }
        for (int i=0;i<12;i++) {
            int from=move.ep[i]; target.ep[i]=source.ep[from];
            target.eo[i]=(byte)(source.eo[from]^move.eo[i]);
        }
    }

    private static Vector3Int Rotate(Vector3Int p, int axis, int direction)
    {
        if (axis==0) return direction==1 ? new Vector3Int(p.x,-p.z,p.y) : new Vector3Int(p.x,p.z,-p.y);
        if (axis==1) return direction==1 ? new Vector3Int(p.z,p.y,-p.x) : new Vector3Int(-p.z,p.y,p.x);
        return direction==1 ? new Vector3Int(-p.y,p.x,p.z) : new Vector3Int(p.y,-p.x,p.z);
    }
    private static int Component(Vector3Int p,int axis) { return axis==0?p.x:axis==1?p.y:p.z; }
    private static State[] BuildMoves()
    {
        var result=new State[18];
        int[] axes={1,1,0,0,2,2}, layers={1,-1,1,-1,1,-1}, directions={1,-1,1,-1,-1,1};
        for (int face=0;face<6;face++) {
            var quarter=new State(); int axis=axes[face];
            for (int i=0;i<8;i++) {
                Vector3Int p=CornerPositions[i]; if (Component(p,axis)!=layers[face]) continue;
                int dest=Array.IndexOf(CornerPositions,Rotate(p,axis,directions[face]));
                quarter.cp[dest]=(byte)i;
                quarter.co[dest]=(byte)(axis==0?(p.y==p.z?1:2):axis==2?(p.x==p.y?2:1):0);
            }
            for (int i=0;i<12;i++) {
                Vector3Int p=EdgePositions[i]; if (Component(p,axis)!=layers[face]) continue;
                int dest=Array.IndexOf(EdgePositions,Rotate(p,axis,directions[face]));
                quarter.ep[dest]=(byte)i; quarter.eo[dest]=(byte)(axis==2?1:0);
            }
            result[face*3]=quarter;
            result[face*3+1]=new State(); Apply(quarter,quarter,result[face*3+1]);
            result[face*3+2]=new State(); Apply(result[face*3+1],quarter,result[face*3+2]);
        }
        return result;
    }

    private static int Twist(State s) { int v=0; for(int i=0;i<7;i++)v=v*3+s.co[i]; return v; }
    private static void SetTwist(State s,int v) {
        int sum=0;for(int i=6;i>=0;i--){s.co[i]=(byte)(v%3);sum+=s.co[i];v/=3;}s.co[7]=(byte)((3-sum%3)%3);
    }
    private static int Flip(State s) { int v=0;for(int i=0;i<11;i++)v=v*2+s.eo[i];return v; }
    private static void SetFlip(State s,int v) {
        int sum=0;for(int i=10;i>=0;i--){s.eo[i]=(byte)(v%2);sum+=s.eo[i];v/=2;}s.eo[11]=(byte)(sum%2);
    }
    private static int Rank(byte[] permutation,int offset,int count,int subtract=0) {
        int rank=0;for(int i=0;i<count;i++) {
            int smaller=0;for(int j=i+1;j<count;j++)if(permutation[offset+j]<permutation[offset+i])smaller++;
            rank+=smaller*Factorial[count-1-i];
        }return rank;
    }
    private static void Unrank(byte[] target,int offset,int count,int rank,int add=0) {
        // A bit mask avoids per-coordinate allocation.
        int available=(1<<count)-1;
        for(int i=0;i<count;i++) {
            int ordinal=rank/Factorial[count-1-i];rank%=Factorial[count-1-i];
            int value=0;for(;value<count;value++)if((available&(1<<value))!=0){if(ordinal==0)break;ordinal--;}
            target[offset+i]=(byte)(value+add);available&=~(1<<value);
        }
    }
    private static int Parity(byte[] p) { int v=0;for(int i=0;i<p.Length;i++)for(int j=i+1;j<p.Length;j++)if(p[i]>p[j])v^=1;return v; }
    private static bool TryImport(SolverState source,out State state)
    {
        state=null;if(source==null||!source.IsValid())return false;
        var s=new State();int cm=0,em=0;
        foreach(SolverPieceState p in source.corners) {
            int slot=Array.IndexOf(CornerPositions,p.position),id=Array.IndexOf(CornerIDs,p.pieceID);
            if(slot<0||id<0||(cm&(1<<slot))!=0)return false;
            cm|=1<<slot;s.cp[slot]=(byte)id;s.co[slot]=(byte)p.orientation;
        }
        foreach(SolverPieceState p in source.edges) {
            int slot=Array.IndexOf(EdgePositions,p.position),id=Array.IndexOf(EdgeIDs,p.pieceID);
            if(slot<0||id<0||(em&(1<<slot))!=0)return false;
            em|=1<<slot;s.ep[slot]=(byte)id;s.eo[slot]=(byte)p.orientation;
        }
        if(cm!=255||em!=4095||Parity(s.cp)!=Parity(s.ep))return false;
        state=s;return true;
    }

    // Compare all 18 generated moves with the project's public state model.
    // This also covers half turns and inverse turns on an already scrambled state.
    public static bool VerifyMoveConvention(SolverState source)
    {
        State imported;
        if (!TryImport(source, out imported)) return false;
        State[] moves = BuildMoves();
        for (int move = 0; move < 18; move++)
        {
            SolverState reference = source.Clone();
            var tokens = new List<string>();
            AppendQuarterTurns(tokens, move);
            foreach (string token in tokens) if (!reference.ApplyMove(token)) return false;
            State expected;
            if (!TryImport(reference, out expected)) return false;
            var actual = new State();
            Apply(imported, moves[move], actual);
            for (int j = 0; j < 8; j++)
                if (actual.cp[j] != expected.cp[j] || actual.co[j] != expected.co[j]) return false;
            for (int j = 0; j < 12; j++)
                if (actual.ep[j] != expected.ep[j] || actual.eo[j] != expected.eo[j]) return false;
        }
        return true;
    }

    private sealed class Tables
    {
        public bool Ready;
        public string Stage="Zugtabellen";
        public State[] Moves=BuildMoves();
        public readonly int[] SliceMasks=new int[495],MaskIndex=new int[4096];
        public int GoalSlice;
        public ushort[] TwistMove=new ushort[2187*18],FlipMove=new ushort[2048*18],SliceMove=new ushort[495*18];
        public ushort[] CornerMove=new ushort[40320*10],EdgeMove=new ushort[40320*10],MiddleMove=new ushort[24*10];
        public byte[] TwistSlice,FlipSlice,CornerMiddle,EdgeMiddle;
        public Tables() {
            int index=0;for(int mask=0;mask<4096;mask++) {
                int bits=0;for(int b=0;b<12;b++)if((mask&(1<<b))!=0)bits++;
                if(bits==4){SliceMasks[index]=mask;MaskIndex[mask]=index++;}
            }GoalSlice=MaskIndex[0xF00];
        }
        public int Slice(State s) { int mask=0;for(int i=0;i<12;i++)if(s.ep[i]>=8)mask|=1<<i;return MaskIndex[mask]; }
        private void SetSlice(State s,int index) {
            int ordinary=0,middle=8,mask=SliceMasks[index];
            for(int i=0;i<12;i++)s.ep[i]=(byte)((mask&(1<<i))!=0?middle++:ordinary++);
        }
        public IEnumerator<int> Build() {
            var input=new State();var output=new State();
            for(int group=0;group<6;group++) {
                int count=group==0?2187:group==1?2048:group==2?495:group==5?24:40320;
                int moveCount=group<3?18:10;
                ushort[] table=group==0?TwistMove:group==1?FlipMove:group==2?SliceMove:group==3?CornerMove:group==4?EdgeMove:MiddleMove;
                Stage="Zugtabelle "+(group+1)+"/6";
                for(int value=0;value<count;value++) {
                    input.Reset();
                    if(group==0)SetTwist(input,value);
                    else if(group==1)SetFlip(input,value);
                    else if(group==2)SetSlice(input,value);
                    else if(group==3)Unrank(input.cp,0,8,value);
                    else if(group==4)Unrank(input.ep,0,8,value);
                    else Unrank(input.ep,8,4,value,8);
                    for(int m=0;m<moveCount;m++) {
                        Apply(input,Moves[group<3?m:Phase2Moves[m]],output);
                        int next=group==0?Twist(output):group==1?Flip(output):group==2?Slice(output):
                            group==3?Rank(output.cp,0,8):group==4?Rank(output.ep,0,8):Rank(output.ep,8,4);
                        table[value*moveCount+m]=(ushort)next;
                    }
                    yield return 0;
                }
            }
            Stage="Distanztabelle 1/4";TwistSlice=new byte[2187*495];
            foreach(int tick in BuildPrune(TwistSlice,TwistMove,SliceMove,495,18,GoalSlice))yield return tick;
            Stage="Distanztabelle 2/4";FlipSlice=new byte[2048*495];
            foreach(int tick in BuildPrune(FlipSlice,FlipMove,SliceMove,495,18,GoalSlice))yield return tick;
            Stage="Distanztabelle 3/4";CornerMiddle=new byte[40320*24];
            foreach(int tick in BuildPrune(CornerMiddle,CornerMove,MiddleMove,24,10,0))yield return tick;
            Stage="Distanztabelle 4/4";EdgeMiddle=new byte[40320*24];
            foreach(int tick in BuildPrune(EdgeMiddle,EdgeMove,MiddleMove,24,10,0))yield return tick;
            Ready=true;Stage="Bereit";
        }
    }
    private static IEnumerable<int> BuildPrune(byte[] distances,ushort[] aMove,ushort[] bMove,int bCount,int moves,int goal) {
        for(int i=0;i<distances.Length;i++){distances[i]=255;if((i&4095)==0)yield return 0;}
        var queue=new int[distances.Length];int head=0,tail=0;queue[tail++]=goal;distances[goal]=0;
        while(head<tail) {
            int value=queue[head++],a=value/bCount,b=value%bCount;byte distance=(byte)(distances[value]+1);
            for(int m=0;m<moves;m++) {
                int next=aMove[a*moves+m]*bCount+bMove[b*moves+m];
                if(distances[next]==255){distances[next]=distance;queue[tail++]=next;}
            }
            yield return 0;
        }
        if(tail!=distances.Length)throw new InvalidOperationException("Distanzkoordinaten nicht vollständig erreichbar.");
    }
    private static bool PruneFace(int face,int previous) {
        return face==previous || (previous>=0 && face/2==previous/2 && face<previous);
    }
    public static SearchSession CreateSearchSession(SolverState state,double maxSearchSeconds=30.0,long maxNodes=5000000) {
        return new SearchSession(state,maxSearchSeconds,maxNodes);
    }
    public sealed class SearchSession : IDisposable
    {
        private IEnumerator<int> routine;
        private readonly SolverState source;
        private readonly double seconds;
        private readonly long maxNodes;
        private readonly Stopwatch searchTimer=new Stopwatch();
        private State[] stack;
        private readonly int[] path1=new int[12],path2=new int[18];
        private int firstLength,secondLength;
        public CubeSolver.SearchStatus Status {get;private set;}=CubeSolver.SearchStatus.Running;
        public List<string> Solution {get;private set;}
        public bool IsComplete => Status!=CubeSolver.SearchStatus.Running;
        public bool IsPreparing => !TablesReady && !IsComplete;
        public string PreparationStage => sharedTables==null?"Start":sharedTables.Stage;
        public long ExpandedStates {get;private set;}
        public int StepCount {get;private set;}
        public double ActiveSearchSeconds => searchTimer.Elapsed.TotalSeconds;
        internal SearchSession(SolverState state,double seconds,long maxNodes) {
            source=state==null?null:state.Clone();this.seconds=seconds;this.maxNodes=maxNodes;routine=Run();
        }
        public void Step(double milliseconds=3.0,int maxWorkItems=4096) {
            if(IsComplete)return;
            if(milliseconds<=0||double.IsNaN(milliseconds)||double.IsInfinity(milliseconds)||maxWorkItems<=0)
                throw new ArgumentOutOfRangeException("milliseconds");
            StepCount++;var slice=Stopwatch.StartNew();
            try {
                for(int i=0;i<maxWorkItems&&!IsComplete;i++) {
                    if(TablesReady)searchTimer.Start();
                    if(!routine.MoveNext()) {if(!IsComplete)Status=CubeSolver.SearchStatus.InvalidInput;break;}
                    if(slice.Elapsed.TotalMilliseconds>=milliseconds)break;
                }
            }catch(Exception exception) {
                if(!TablesReady) {
                    if(sharedBuilder!=null)sharedBuilder.Dispose();
                    sharedBuilder=null;sharedTables=null;
                }
                Status=CubeSolver.SearchStatus.InvalidInput;
                Debug.LogError("LONG SOLVER: "+exception.GetType().Name+": "+exception.Message);
            }finally {
                searchTimer.Stop();if(IsComplete)Release();
            }
        }
        public void Cancel() {if(IsComplete)return;Status=CubeSolver.SearchStatus.Cancelled;searchTimer.Stop();Release();}
        public void Dispose() {if(!IsComplete)Cancel();Release();}
        private void Release() {if(routine!=null){routine.Dispose();routine=null;}stack=null;}
        private bool Stop() {
            if(Status!=CubeSolver.SearchStatus.Running)return true;
            if(searchTimer.Elapsed.TotalSeconds>=seconds){Status=CubeSolver.SearchStatus.TimeLimit;return true;}
            if(ExpandedStates>=maxNodes){Status=CubeSolver.SearchStatus.StateLimit;return true;}return false;
        }
        private IEnumerator<int> Run() {
            State imported;
            if(seconds<=0||double.IsNaN(seconds)||double.IsInfinity(seconds)||maxNodes<1||!TryImport(source,out imported)) {
                Status=CubeSolver.SearchStatus.InvalidInput;
                Debug.LogWarning("LONG SOLVER: Ungültiger Zustand oder Grenzwert; nur äußere Ebenen unterstützt.");yield break;
            }
            if(CubeSolver.IsSolved(source)){Solution=new List<string>();Status=CubeSolver.SearchStatus.Solved;yield break;}
            if(sharedTables==null){
                if(!VerifyMoveConvention(source))throw new InvalidOperationException("Zugkonvention stimmt nicht mit SolverState überein.");
                sharedTables=new Tables();sharedBuilder=sharedTables.Build();
            }
            while(!sharedTables.Ready) {
                // Shared preparation resumes after cancellation; all calls run on Unity's main thread.
                if(!sharedBuilder.MoveNext()&&!sharedTables.Ready)throw new InvalidOperationException("Tabellenaufbau unvollständig.");
                if(sharedTables.Ready){sharedBuilder.Dispose();sharedBuilder=null;}
                yield return 0;
            }
            stack=new State[13];stack[0]=imported;for(int i=1;i<stack.Length;i++)stack[i]=new State();
            int twist=Twist(imported),flip=Flip(imported),slice=sharedTables.Slice(imported);
            int lower=Math.Max(sharedTables.TwistSlice[twist*495+slice],sharedTables.FlipSlice[flip*495+slice]);
            for(int depth=lower;depth<=12&&!Stop();depth++) {
                firstLength=depth;
                foreach(int tick in SearchFirst(0,depth,-1,twist,flip,slice)){yield return tick;if(Stop())yield break;}
            }
            if(Status==CubeSolver.SearchStatus.Running)Status=CubeSolver.SearchStatus.DepthLimit;
        }
        private IEnumerable<int> SearchFirst(int level,int remaining,int previous,int twist,int flip,int slice) {
            yield return 0;if(Stop())yield break;ExpandedStates++;
            int h=Math.Max(sharedTables.TwistSlice[twist*495+slice],sharedTables.FlipSlice[flip*495+slice]);
            if(h>remaining)yield break;
            if(remaining==0) {
                if(h!=0)yield break;
                State state=stack[level];int corners=Rank(state.cp,0,8),edges=Rank(state.ep,0,8),middle=Rank(state.ep,8,4);
                int lower=Math.Max(sharedTables.CornerMiddle[corners*24+middle],sharedTables.EdgeMiddle[edges*24+middle]);
                for(int depth=lower;depth<=18&&!Stop();depth++) {
                    secondLength=depth;
                    foreach(int tick in SearchSecond(0,depth,-1,corners,edges,middle)){yield return tick;if(Stop())yield break;}
                }yield break;
            }
            for(int move=0;move<18;move++) {
                int face=move/3;if(PruneFace(face,previous))continue;
                int nextTwist=sharedTables.TwistMove[twist*18+move],nextFlip=sharedTables.FlipMove[flip*18+move],
                    nextSlice=sharedTables.SliceMove[slice*18+move];
                int nextH=Math.Max(sharedTables.TwistSlice[nextTwist*495+nextSlice],sharedTables.FlipSlice[nextFlip*495+nextSlice]);
                if(nextH>remaining-1)continue;
                Apply(stack[level],sharedTables.Moves[move],stack[level+1]);path1[level]=move;
                foreach(int tick in SearchFirst(level+1,remaining-1,face,nextTwist,nextFlip,nextSlice)){yield return tick;if(Stop())yield break;}
            }
        }
        private IEnumerable<int> SearchSecond(int level,int remaining,int previous,int corners,int edges,int middle) {
            yield return 0;if(Stop())yield break;ExpandedStates++;
            int h=Math.Max(sharedTables.CornerMiddle[corners*24+middle],sharedTables.EdgeMiddle[edges*24+middle]);
            if(h>remaining)yield break;
            if(remaining==0) {
                if(corners==0&&edges==0&&middle==0) {
                    var result=new List<string>();
                    for(int i=0;i<firstLength;i++)AppendQuarterTurns(result,path1[i]);
                    for(int i=0;i<secondLength;i++)AppendQuarterTurns(result,path2[i]);
                    // Final independent verification uses the unchanged public SolverState.
                    SolverState verification=source.Clone();bool valid=true;
                    foreach(string move in result)valid=verification.ApplyMove(move)&&valid;
                    if(!valid||!verification.IsValid()||!CubeSolver.IsSolved(verification))
                        throw new InvalidOperationException("Gefundene Lösung stimmt nicht mit SolverState überein.");
                    Solution=result;Status=CubeSolver.SearchStatus.Solved;
                }yield break;
            }
            for(int m=0;m<10;m++) {
                int move=Phase2Moves[m],face=move/3;if(PruneFace(face,previous))continue;
                int c=sharedTables.CornerMove[corners*10+m],e=sharedTables.EdgeMove[edges*10+m],s=sharedTables.MiddleMove[middle*10+m];
                if(Math.Max(sharedTables.CornerMiddle[c*24+s],sharedTables.EdgeMiddle[e*24+s])>remaining-1)continue;
                path2[level]=move;
                foreach(int tick in SearchSecond(level+1,remaining-1,face,c,e,s)){yield return tick;if(Stop())yield break;}
            }
        }
    }
    private static void AppendQuarterTurns(List<string> path,int move) {
        string face=Faces[move/3];int power=move%3;
        if(power==0)path.Add(face);else if(power==1){path.Add(face);path.Add(face);}else path.Add(face+"'");
    }
}
