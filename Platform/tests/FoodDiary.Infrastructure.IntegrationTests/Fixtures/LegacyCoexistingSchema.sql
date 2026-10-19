-- Frozen schema-only coexistence snapshot for the pre-EF Prisma application
-- and the retired shared-database MailRelay tables. Keep these objects in place.
-- Contains no application rows, credentials, grants or sequence values.
-- Load after PreBaseline.sql; application migrations continue to own only their EF tables.
CREATE TYPE public."Unit" AS ENUM ('G', 'ML', 'PCS');
CREATE TYPE public."Visibility" AS ENUM ('PUBLIC', 'PRIVATE');
--
-- PostgreSQL database dump
--


-- Dumped from database version 17.11
-- Dumped by pg_dump version 17.11

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: Consumption; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Consumption" (
    id integer NOT NULL,
    "userId" integer NOT NULL,
    date timestamp(3) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    comment text,
    "createdAt" timestamp(3) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "updatedAt" timestamp(3) without time zone NOT NULL
);


--
-- Name: ConsumptionItem; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."ConsumptionItem" (
    id integer NOT NULL,
    "consumptionId" integer NOT NULL,
    "foodId" integer NOT NULL,
    amount double precision NOT NULL,
    "createdAt" timestamp(3) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "updatedAt" timestamp(3) without time zone NOT NULL
);


--
-- Name: ConsumptionItem_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."ConsumptionItem_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: ConsumptionItem_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."ConsumptionItem_id_seq" OWNED BY public."ConsumptionItem".id;


--
-- Name: Consumption_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."Consumption_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Consumption_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."Consumption_id_seq" OWNED BY public."Consumption".id;


--
-- Name: Food; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Food" (
    id integer NOT NULL,
    barcode text,
    name text NOT NULL,
    category text,
    "baseUnit" public."Unit" NOT NULL,
    "baseAmount" double precision NOT NULL,
    "caloriesPerBase" double precision NOT NULL,
    "proteinsPerBase" double precision NOT NULL,
    "fatsPerBase" double precision NOT NULL,
    "carbsPerBase" double precision NOT NULL,
    "usageCount" integer DEFAULT 0 NOT NULL,
    visibility public."Visibility" DEFAULT 'PUBLIC'::public."Visibility" NOT NULL,
    "createdAt" timestamp(3) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "updatedAt" timestamp(3) without time zone NOT NULL,
    "userId" integer NOT NULL
);


--
-- Name: Food_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."Food_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Food_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."Food_id_seq" OWNED BY public."Food".id;


--
-- Name: Recipe; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Recipe" (
    id integer NOT NULL,
    name text NOT NULL,
    description text,
    category text,
    "imageUrl" text,
    "prepTime" integer,
    "cookTime" integer,
    servings integer NOT NULL,
    "totalCalories" double precision,
    "totalProteins" double precision,
    "totalFats" double precision,
    "totalCarbs" double precision,
    "createdAt" timestamp(3) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "updatedAt" timestamp(3) without time zone NOT NULL,
    "userId" integer NOT NULL
);


--
-- Name: RecipeItem; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."RecipeItem" (
    id integer NOT NULL,
    "recipeId" integer NOT NULL,
    "foodId" integer NOT NULL,
    amount double precision NOT NULL,
    unit public."Unit" NOT NULL,
    "createdAt" timestamp(3) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "updatedAt" timestamp(3) without time zone NOT NULL
);


--
-- Name: RecipeItem_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."RecipeItem_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: RecipeItem_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."RecipeItem_id_seq" OWNED BY public."RecipeItem".id;


--
-- Name: RecipeStep; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."RecipeStep" (
    id integer NOT NULL,
    "recipeId" integer NOT NULL,
    "stepNumber" integer NOT NULL,
    description text NOT NULL,
    "imageUrl" text,
    "createdAt" timestamp(3) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "updatedAt" timestamp(3) without time zone NOT NULL
);


--
-- Name: RecipeStepItem; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."RecipeStepItem" (
    id integer NOT NULL,
    "stepId" integer NOT NULL,
    "recipeItemId" integer NOT NULL,
    "createdAt" timestamp(3) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "updatedAt" timestamp(3) without time zone NOT NULL
);


--
-- Name: RecipeStepItem_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."RecipeStepItem_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: RecipeStepItem_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."RecipeStepItem_id_seq" OWNED BY public."RecipeStepItem".id;


--
-- Name: RecipeStep_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."RecipeStep_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: RecipeStep_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."RecipeStep_id_seq" OWNED BY public."RecipeStep".id;


--
-- Name: Recipe_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."Recipe_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Recipe_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."Recipe_id_seq" OWNED BY public."Recipe".id;


--
-- Name: User; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."User" (
    id integer NOT NULL,
    email text NOT NULL,
    password text NOT NULL,
    "refreshToken" text,
    username text,
    "firstName" text,
    "lastName" text,
    "birthDate" timestamp(3) without time zone,
    gender text,
    weight double precision,
    height double precision,
    "profileImage" text,
    "isActive" boolean DEFAULT true NOT NULL,
    "createdAt" timestamp(3) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "updatedAt" timestamp(3) without time zone NOT NULL
);


--
-- Name: User_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."User_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: User_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."User_id_seq" OWNED BY public."User".id;


--
-- Name: _prisma_migrations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public._prisma_migrations (
    id character varying(36) NOT NULL,
    checksum character varying(64) NOT NULL,
    finished_at timestamp with time zone,
    migration_name character varying(255) NOT NULL,
    logs text,
    rolled_back_at timestamp with time zone,
    started_at timestamp with time zone DEFAULT now() NOT NULL,
    applied_steps_count integer DEFAULT 0 NOT NULL
);


--
-- Name: mailrelay_delivery_events; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.mailrelay_delivery_events (
    id uuid NOT NULL,
    event_type text NOT NULL,
    email text NOT NULL,
    source text NOT NULL,
    classification text,
    provider_message_id text,
    reason text,
    occurred_at_utc timestamp with time zone NOT NULL,
    created_at_utc timestamp with time zone NOT NULL
);


--
-- Name: mailrelay_inbox_messages; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.mailrelay_inbox_messages (
    id uuid NOT NULL,
    consumer_name text NOT NULL,
    message_key text NOT NULL,
    status text NOT NULL,
    locked_at_utc timestamp with time zone NOT NULL,
    processed_at_utc timestamp with time zone,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    last_error text
);


--
-- Name: mailrelay_outbound_emails; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.mailrelay_outbound_emails (
    id uuid NOT NULL,
    status text NOT NULL,
    from_address text NOT NULL,
    from_name text NOT NULL,
    to_recipients_json jsonb NOT NULL,
    subject text NOT NULL,
    html_body text NOT NULL,
    text_body text,
    correlation_id text,
    idempotency_key text,
    attempt_count integer DEFAULT 0 NOT NULL,
    max_attempts integer NOT NULL,
    available_at_utc timestamp with time zone NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    locked_at_utc timestamp with time zone,
    sent_at_utc timestamp with time zone,
    last_error text
);


--
-- Name: mailrelay_outbox_messages; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.mailrelay_outbox_messages (
    id uuid NOT NULL,
    email_id uuid NOT NULL,
    status text NOT NULL,
    attempt_count integer DEFAULT 0 NOT NULL,
    available_at_utc timestamp with time zone NOT NULL,
    locked_at_utc timestamp with time zone,
    published_at_utc timestamp with time zone,
    created_at_utc timestamp with time zone NOT NULL,
    last_error text
);


--
-- Name: mailrelay_suppressions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.mailrelay_suppressions (
    email text NOT NULL,
    reason text NOT NULL,
    source text NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    expires_at_utc timestamp with time zone
);


--
-- Name: Consumption id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Consumption" ALTER COLUMN id SET DEFAULT nextval('public."Consumption_id_seq"'::regclass);


--
-- Name: ConsumptionItem id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ConsumptionItem" ALTER COLUMN id SET DEFAULT nextval('public."ConsumptionItem_id_seq"'::regclass);


--
-- Name: Food id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Food" ALTER COLUMN id SET DEFAULT nextval('public."Food_id_seq"'::regclass);


--
-- Name: Recipe id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Recipe" ALTER COLUMN id SET DEFAULT nextval('public."Recipe_id_seq"'::regclass);


--
-- Name: RecipeItem id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RecipeItem" ALTER COLUMN id SET DEFAULT nextval('public."RecipeItem_id_seq"'::regclass);


--
-- Name: RecipeStep id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RecipeStep" ALTER COLUMN id SET DEFAULT nextval('public."RecipeStep_id_seq"'::regclass);


--
-- Name: RecipeStepItem id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RecipeStepItem" ALTER COLUMN id SET DEFAULT nextval('public."RecipeStepItem_id_seq"'::regclass);


--
-- Name: User id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."User" ALTER COLUMN id SET DEFAULT nextval('public."User_id_seq"'::regclass);


--
-- Name: ConsumptionItem ConsumptionItem_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ConsumptionItem"
    ADD CONSTRAINT "ConsumptionItem_pkey" PRIMARY KEY (id);


--
-- Name: Consumption Consumption_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Consumption"
    ADD CONSTRAINT "Consumption_pkey" PRIMARY KEY (id);


--
-- Name: Food Food_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Food"
    ADD CONSTRAINT "Food_pkey" PRIMARY KEY (id);


--
-- Name: RecipeItem RecipeItem_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RecipeItem"
    ADD CONSTRAINT "RecipeItem_pkey" PRIMARY KEY (id);


--
-- Name: RecipeStepItem RecipeStepItem_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RecipeStepItem"
    ADD CONSTRAINT "RecipeStepItem_pkey" PRIMARY KEY (id);


--
-- Name: RecipeStep RecipeStep_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RecipeStep"
    ADD CONSTRAINT "RecipeStep_pkey" PRIMARY KEY (id);


--
-- Name: Recipe Recipe_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Recipe"
    ADD CONSTRAINT "Recipe_pkey" PRIMARY KEY (id);


--
-- Name: User User_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."User"
    ADD CONSTRAINT "User_pkey" PRIMARY KEY (id);


--
-- Name: _prisma_migrations _prisma_migrations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public._prisma_migrations
    ADD CONSTRAINT _prisma_migrations_pkey PRIMARY KEY (id);


--
-- Name: mailrelay_delivery_events mailrelay_delivery_events_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.mailrelay_delivery_events
    ADD CONSTRAINT mailrelay_delivery_events_pkey PRIMARY KEY (id);


--
-- Name: mailrelay_inbox_messages mailrelay_inbox_messages_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.mailrelay_inbox_messages
    ADD CONSTRAINT mailrelay_inbox_messages_pkey PRIMARY KEY (id);


--
-- Name: mailrelay_outbound_emails mailrelay_outbound_emails_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.mailrelay_outbound_emails
    ADD CONSTRAINT mailrelay_outbound_emails_pkey PRIMARY KEY (id);


--
-- Name: mailrelay_outbox_messages mailrelay_outbox_messages_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.mailrelay_outbox_messages
    ADD CONSTRAINT mailrelay_outbox_messages_pkey PRIMARY KEY (id);


--
-- Name: mailrelay_suppressions mailrelay_suppressions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.mailrelay_suppressions
    ADD CONSTRAINT mailrelay_suppressions_pkey PRIMARY KEY (email);


--
-- Name: User_email_key; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "User_email_key" ON public."User" USING btree (email);


--
-- Name: ix_mailrelay_delivery_events_email_created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_mailrelay_delivery_events_email_created ON public.mailrelay_delivery_events USING btree (email, created_at_utc DESC);


--
-- Name: ix_mailrelay_outbound_emails_due; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_mailrelay_outbound_emails_due ON public.mailrelay_outbound_emails USING btree (status, available_at_utc, created_at_utc);


--
-- Name: ix_mailrelay_outbound_emails_processing; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_mailrelay_outbound_emails_processing ON public.mailrelay_outbound_emails USING btree (status, locked_at_utc);


--
-- Name: ix_mailrelay_outbox_messages_due; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX ix_mailrelay_outbox_messages_due ON public.mailrelay_outbox_messages USING btree (status, available_at_utc, created_at_utc);


--
-- Name: ux_mailrelay_inbox_messages_consumer_message_key; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ux_mailrelay_inbox_messages_consumer_message_key ON public.mailrelay_inbox_messages USING btree (consumer_name, message_key);


--
-- Name: ux_mailrelay_outbound_emails_idempotency_key; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ux_mailrelay_outbound_emails_idempotency_key ON public.mailrelay_outbound_emails USING btree (idempotency_key) WHERE (idempotency_key IS NOT NULL);


--
-- Name: ConsumptionItem ConsumptionItem_consumptionId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ConsumptionItem"
    ADD CONSTRAINT "ConsumptionItem_consumptionId_fkey" FOREIGN KEY ("consumptionId") REFERENCES public."Consumption"(id) ON UPDATE CASCADE ON DELETE RESTRICT;


--
-- Name: ConsumptionItem ConsumptionItem_foodId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ConsumptionItem"
    ADD CONSTRAINT "ConsumptionItem_foodId_fkey" FOREIGN KEY ("foodId") REFERENCES public."Food"(id) ON UPDATE CASCADE ON DELETE RESTRICT;


--
-- Name: Consumption Consumption_userId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Consumption"
    ADD CONSTRAINT "Consumption_userId_fkey" FOREIGN KEY ("userId") REFERENCES public."User"(id) ON UPDATE CASCADE ON DELETE RESTRICT;


--
-- Name: Food Food_userId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Food"
    ADD CONSTRAINT "Food_userId_fkey" FOREIGN KEY ("userId") REFERENCES public."User"(id) ON UPDATE CASCADE ON DELETE RESTRICT;


--
-- Name: RecipeItem RecipeItem_foodId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RecipeItem"
    ADD CONSTRAINT "RecipeItem_foodId_fkey" FOREIGN KEY ("foodId") REFERENCES public."Food"(id) ON UPDATE CASCADE ON DELETE RESTRICT;


--
-- Name: RecipeItem RecipeItem_recipeId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RecipeItem"
    ADD CONSTRAINT "RecipeItem_recipeId_fkey" FOREIGN KEY ("recipeId") REFERENCES public."Recipe"(id) ON UPDATE CASCADE ON DELETE RESTRICT;


--
-- Name: RecipeStepItem RecipeStepItem_recipeItemId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RecipeStepItem"
    ADD CONSTRAINT "RecipeStepItem_recipeItemId_fkey" FOREIGN KEY ("recipeItemId") REFERENCES public."RecipeItem"(id) ON UPDATE CASCADE ON DELETE RESTRICT;


--
-- Name: RecipeStepItem RecipeStepItem_stepId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RecipeStepItem"
    ADD CONSTRAINT "RecipeStepItem_stepId_fkey" FOREIGN KEY ("stepId") REFERENCES public."RecipeStep"(id) ON UPDATE CASCADE ON DELETE RESTRICT;


--
-- Name: RecipeStep RecipeStep_recipeId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RecipeStep"
    ADD CONSTRAINT "RecipeStep_recipeId_fkey" FOREIGN KEY ("recipeId") REFERENCES public."Recipe"(id) ON UPDATE CASCADE ON DELETE RESTRICT;


--
-- Name: Recipe Recipe_userId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Recipe"
    ADD CONSTRAINT "Recipe_userId_fkey" FOREIGN KEY ("userId") REFERENCES public."User"(id) ON UPDATE CASCADE ON DELETE RESTRICT;


--
-- PostgreSQL database dump complete
--


SET search_path TO public;
