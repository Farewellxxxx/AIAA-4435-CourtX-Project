namespace CourtX
{
    public sealed partial class TacticalSession
    {
        LearningHub learning;
        public LearningHub Learning=>learning??(learning=new LearningHub(this));
        public void OpenLearning(LearningPage page=LearningPage.Lesson)=>Learning.Show(page);
    }
}
