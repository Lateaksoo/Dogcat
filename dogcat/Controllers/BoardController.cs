﻿using dogcat.Common;
using dogcat.Models.Domain;
using dogcat.Models.ViewModels;
using dogcat.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace dogcat.Controllers
{
    public class BoardController : Controller
    {
        private readonly IWriteRepository writeRepository;
        private readonly ICommentRepository commentRepository;

        public BoardController(IWriteRepository writeRepository, ICommentRepository commentRepository)
        {
            Console.WriteLine("BoardController() 생성");
            this.writeRepository = writeRepository;
            Console.WriteLine("BoardController() 생성");
            this.commentRepository = commentRepository;
        }

        //User 여부 판별 
        // Get: /Board/IsUser
        [HttpGet]
        [ActionName("IsUser")]
        public IActionResult IsUser(string buttonName)
        {
            //Session 에 저장된 회원 정보를 가져온다
            int? userId = HttpContext.Session.GetInt32("userId");
            //가져온 회원정보로 글쓰기에 접근하는 사람이 회원인지 확인한다.

            //유저라면
            if (userId != null)
            {
                if (buttonName == "Write")
                {
                    return View("IsUser", 1);  // View(string, object) => viewname, model
                                               // 작성 버튼인 경우의 동작 수행
                                               // 필요한 처리를 수행
                }
                else if (buttonName == "Detail")
                {
                    return View("IsUser", 2);
                    // 상세보기 버튼인 경우의 동작 수행
                    // 필요한 처리를 수행
                    // writeId를 사용하여 추가 작업 수행 가능
                }
            }
            //비회원이라면 
            else
            {
                //글쓰기 버튼 누르면 
                if (buttonName == "Write")
                {
                    return View("IsUser", 0);
                }
                //디테일 버튼 누르면 
                else if (buttonName == "Detail")
                {
                    return View("IsUser", 3);
                   
                }
            }
            return View("IsUser", 0);
        }

        // GET: /Board/Write
        [HttpGet]
        public IActionResult Write()
        {
            ViewData["page"] = HttpContext.Session.GetInt32("page") ?? 1;
            addWriteRequest addWriteRequest = new()
            {
                NickName = HttpContext.Session.GetString("userNickName")

            };
            return View(addWriteRequest);
        }

        // POST: /Board/Write
        [HttpPost]
        [ActionName("Write")]
        public async Task<IActionResult> Add(addWriteRequest addWriteRequest)
        {
            //// Validation
            //addWriteRequest.Validate();
            //if (addWriteRequest.HasError)
            //{
            //    TempData["NameError"] = addWriteRequest.ErrorName;
            //    TempData["SubjectError"] = addWriteRequest.ErrorSubject;

            //    // 사용자가 입력했던 데이터
            //    TempData["Name"] = addWriteRequest.Name;
            //    TempData["Subject"] = addWriteRequest.Subject;
            //    TempData["Content"] = addWriteRequest.Content;

            //    return RedirectToAction("Write");
            //}
            int userId = HttpContext.Session.GetInt32("userId") ?? 0;
            var write = new Write
            {
                UserId = userId,
                NickName = addWriteRequest.NickName,
                Category = addWriteRequest.Category,
                Title = addWriteRequest.Title,
                Context = addWriteRequest.Context,
                Time = DateTime.Now,
            };

            write = await writeRepository.AddAsync(write);

            return RedirectToAction("Detail", new { id = write.Id, NickName = write.NickName });
        }
        //댓글작성
        [HttpPost]
        public async Task<IActionResult> AddComment(addCommentRequest addCommentRequest)
        {
            var comment = new Comment
            {
                UserId = addCommentRequest.UserId,
                Content = addCommentRequest.Content,
                WriteId = addCommentRequest.WriteId,
                NickName = addCommentRequest.NickName,
                Time = DateTime.Now,
            };

            comment = await commentRepository.CommnetAddAsync(comment);


            return RedirectToAction("Detail", new { id = addCommentRequest.WriteId });

        }

        // GET: /Board/List
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery(Name = "page")] int? page, // int? 타입.  만약 page parameter 가 없거나, 변환 안되는 값이면 null 값
            string category
            )
        {
            // 현재 페이지 parameter
            page ??= 1;   // 디폴트는 1 page
            long cnt;
            if (page < 1) page = 1;
            //await Console.Out.WriteLineAsync($"page = {page}");  // 확인용

            // 페이징
            // writePages: 한 [페이징] 당 몇개의 페이지가 표시되나
            // pageRows: 한 '페이지'에 몇개의 글을 리스트 할것인가?
            int writePages = HttpContext.Session.GetInt32("writePages") ?? C.WRITE_PAGES;
            int pageRows = HttpContext.Session.GetInt32("pageRows") ?? C.PAGE_ROWS;
            // 현재 페이지 번호 => Session 에 저장
            HttpContext.Session.SetInt32("page", (int)page);

            // 글 목록 전체의 개수
            if (category == null || category == "전체")
            {
               cnt = await writeRepository.CountAsync();
            }
            else
            {
                cnt = await writeRepository.CountCategory(category);
            }
            // 총 몇 '페이지' 분량?
            int totalPage = (int)Math.Ceiling(cnt / (double)pageRows);

            // page 값 보정
            if (page > totalPage) page = totalPage;
            if (page <= 0) page = 1;
            // 몇번째 데이터부터?
            int fromRow = ((int)page - 1) * pageRows;

            // [페이징] 에 표시할 '시작페이지' 와 '마지막 페이지' 계산
            int startPage = ((((int)page - 1) / writePages) * writePages) + 1;
            int endPage = startPage + writePages - 1;
            if (endPage >= totalPage) endPage = totalPage;


            // 위 값들을 View에 보내주기
            ViewData["cnt"] = cnt;  // 전체글개수
            ViewData["page"] = page;  // 현재 페이지
            ViewData["totalPage"] = totalPage;  // 총 '페이지' 수 
            ViewData["pageRows"] = pageRows;  // 한 '페이지' 에 표시할 글 개수

            // [페이징]            
            ViewData["url"] = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}";  // 목록 url
            ViewData["writePages"] = writePages; // [페이징] 에 표시할 숫자 개수
            ViewData["startPage"] = startPage; // [페이징] 에 표시할 시작 페이지
            ViewData["endPage"] = endPage; // [페이징] 에 표시할 마지막 페이지

            ViewData["category"] = category;

            var writes = await writeRepository.GetFromRowAsync(fromRow, pageRows, category);
            return View(writes);
        }

        // GET: /Board/Detail/{id}
        [HttpGet]
        public async Task<IActionResult> Detail(long id)
        {
            // TODO : 특정 id 의 글 읽어오기
            // 조회수 증가
            var write = await writeRepository.IncViewCntAsync(id);
            var comment = await commentRepository.CommentGetAsync(id);
            string nickname = HttpContext.Session.GetString("userNickName");
            int userId = HttpContext.Session.GetInt32("userId") ?? 0;
            // 페이징
            ViewData["page"] = HttpContext.Session.GetInt32("page") ?? 1;

            if (write == null)
            {
                // ※ 에러 메세지 처리 필요
                return RedirectToAction("List");
            }
            if (comment == null)
            {
                comment = new();
            }

            ViewData["Write"] = write;
            ViewData["UserId"] = userId;
            ViewData["Comment"] = comment;
            ViewData["Nickname"] = nickname;

            return View();
        }

        // GET: /Board/Update/{id}
        [HttpGet]
        public async Task<IActionResult> Update(long id)
        {
            // TODO : 특정 id 의 글 읽어오기
            var write = await writeRepository.GetAsync(id);
            if (write == null) return View(null);

            EditWriteRequest writeRequest = new()
            {
                Id = write.Id,
                UserId = write.UserId,
                Title = write.Title,
                Category = write.Category,
                Context = write.Context,
                Time = write.Time,
                ViewCnt = write.ViewCnt,
            };

            ViewData["page"] = HttpContext.Session.GetInt32("page") ?? 1;

            return View(writeRequest);
        }

        // POST: /Board/Update
        [HttpPost]
        public async Task<IActionResult> Update(EditWriteRequest request)
        {
            var write = new Write
            {
                Id = request.Id,
                Title = request.Title,
                Category = request.Category, // 카테고리 값을 업데이트합니다.   
                Context = request.Context,
            };
            var updateWrite = await writeRepository.UpdateAsync(write);

            if (updateWrite == null)
            {
                // 수정 실패하면 List 로
                return RedirectToAction("List");
            }

            return RedirectToAction("Detail", new { id = request.Id });

        }

        // POST: /Board/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            var deletedWrite = await writeRepository.DeleteAsync(id);

            if (deletedWrite != null)
            {
                // 삭제 성공
                return View("DeleteOk", 1);  // View(string, object) => viewname, model
            }

            // 삭제 실패
            return View("DeleteOk", 0);
        }
        // POST: /Board/CmtDelete
        [HttpPost]
        public async Task<IActionResult> CommentDelete(long id)
        {
            var deletedWrite = await commentRepository.CommentDeleteAsync(id);

            if (deletedWrite != null)
            {
                // 삭제 성공
                return View("CommentDeleteOk", 1);  // View(string, object) => viewname, model
            }
            // 삭제 실패
            return View("CommentDeleteOk", 0);

        }

        // 페이징
        // pageRows 값 변경시 동작
        [HttpPost]
        public IActionResult PageRows(int page, int pageRows)
        {
            HttpContext.Session.SetInt32("pageRows", pageRows);
            return RedirectToAction("List", new { page = page });
        }
    }
}
