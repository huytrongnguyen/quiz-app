create table quiz_question (
  question_id text not null,
  content text not null,
  max_response_time int4 not null,
  correct_choice text not null,
  choice1 text not null,
  choice2 text not null,
  choice3 text not null,
  primary key (question_id)
)