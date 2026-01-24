<template>
    <div :class="`${header ? 'bg-primary font-semibold rounded-t-xl' : 'font-medium'} flex items-center  px-4 py-3 border border-[#ECECEC] text-[13px] sm:text-[15px] tracking-tight  line-clamp-1 `">
        <div class="w-[25%] md:w-[10%]">
            <span class="w-[20px] h-[20px]  ">{{ firstCol }}</span>
        </div>
        <div class="w-full">
            <span>{{ secondCol }}</span>
        </div>
        <div class="w-[20%] md:w-[15%]">
            <span>{{ thirdCol }}</span>
        </div>
    </div>
</template>

<script lang='ts' setup>
defineProps<{
  firstCol: string
  secondCol: string
  thirdCol: string
  header?: boolean
}>()
</script>
