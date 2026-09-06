/**
 * 목적: 씨앗 파일 왕복 검증. Read → ToonWriter → 텍스트 == 원본 (바이트 동일).
 * 왜 이 구조인가: TS seed.test.ts 의 C# 포팅. 정규 출력이 깨지면 익스포터와 리더의 계약이 어긋난 증거다.
 * 바꾸면 안 되는 것: 바이트 동일 비교. 허용 범위를 넓히지 마라.
 * 근거: SDD-06 §2 [D-06-02], SDD-05 §11 [D-05-11]
 */
using System.IO;
using System.Text;
using NUnit.Framework;
using PMF.EditorTools.Authoring;

namespace PMF.Tests
{
    public class RoundTripTests
    {
        const string SeedPath = "Assets/_Project/Data/Stages/Stage_Greybox.toon";

        [Test]
        public void SeedReadWrite_RoundTrip_ByteIdentical()
        {
            var original = File.ReadAllText(SeedPath);
            var readResult = StageDocumentReader.Read(original);
            Assert.That(readResult.Ok, Is.True, "씨앗 Read 실패");
            Assert.That(readResult.Issues.Count, Is.EqualTo(0), $"씨앗 Read: 이슈 있음: {string.Join(", ", readResult.Issues)}");

            var written = ToonWriter.Write(readResult.Doc);

            Assert.That(written, Is.EqualTo(original), "Read→Write 결과가 원본과 다릅니다");

            // 바이트 수 검증 (기준은 ToonWriter 쪽)
            var originalBytes = Encoding.UTF8.GetByteCount(original);
            var writtenBytes = Encoding.UTF8.GetByteCount(written);
            Assert.That(writtenBytes, Is.EqualTo(originalBytes), "바이트 수 불일치");
        }
    }
}